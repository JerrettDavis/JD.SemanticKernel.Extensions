using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace JD.SemanticKernel.Extensions.Memory.Sqlite;

/// <summary>
/// SQLite-backed <see cref="IMemoryBackend"/> for persistent semantic memory storage.
/// Uses blob storage for embeddings and performs cosine similarity search in managed code.
/// </summary>
public sealed class SqliteMemoryBackend : IMemoryBackend, IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="SqliteMemoryBackend"/>.
    /// </summary>
    /// <param name="connectionString">SQLite connection string (e.g., "Data Source=memory.db").</param>
    public SqliteMemoryBackend(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or whitespace.", nameof(connectionString));
        }

        _connection = new SqliteConnection(connectionString);
    }

    /// <summary>
    /// Creates a backend with a file-based database.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    /// <returns>A new <see cref="SqliteMemoryBackend"/> instance.</returns>
    public static SqliteMemoryBackend FromFile(string databasePath)
    {
        return new SqliteMemoryBackend($"Data Source={databasePath}");
    }

    /// <inheritdoc />
    public async Task StoreAsync(MemoryRecord record, CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(record);
#else
        if (record is null) throw new ArgumentNullException(nameof(record));
#endif

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO memories (id, text, embedding, metadata, created_at, last_accessed_at)
            VALUES (@id, @text, @embedding, @metadata, @createdAt, @lastAccessedAt)
            """;

        cmd.Parameters.AddWithValue("@id", record.Id);
        cmd.Parameters.AddWithValue("@text", record.Text);
        cmd.Parameters.AddWithValue("@embedding", SerializeEmbedding(record.Embedding));
        cmd.Parameters.AddWithValue("@metadata", SerializeMetadata(record.Metadata));
        cmd.Parameters.AddWithValue("@createdAt", record.CreatedAt.ToString("o", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("@lastAccessedAt", record.LastAccessedAt.ToString("o", CultureInfo.InvariantCulture));

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(MemoryRecord Record, double Score)>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        // Load all records and compute similarity in managed code
        // (SQLite doesn't have native vector ops without extensions)
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, text, embedding, metadata, created_at, last_accessed_at FROM memories";

        var results = new List<(MemoryRecord Record, double Score)>();

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var record = new MemoryRecord
            {
                Id = reader.GetString(0),
                Text = reader.GetString(1),
                Embedding = DeserializeEmbedding((byte[])reader.GetValue(2)),
                Metadata = DeserializeMetadata(reader.GetString(3)),
                CreatedAt = DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                LastAccessedAt = DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
            };

            var score = CosineSimilarity(queryEmbedding, record.Embedding);
            results.Add((record, score));
        }

        return results
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM memories WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM memories WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture) > 0;
    }

    /// <inheritdoc />
    public async Task<MemoryRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, text, embedding, metadata, created_at, last_accessed_at FROM memories WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new MemoryRecord
        {
            Id = reader.GetString(0),
            Text = reader.GetString(1),
            Embedding = DeserializeEmbedding((byte[])reader.GetValue(2)),
            Metadata = DeserializeMetadata(reader.GetString(3)),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
            LastAccessedAt = DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
        };
    }

    /// <summary>Disposes the SQLite connection.</summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Dispose();
            _disposed = true;
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS memories (
                id TEXT PRIMARY KEY,
                text TEXT NOT NULL,
                embedding BLOB NOT NULL,
                metadata TEXT NOT NULL DEFAULT '{}',
                created_at TEXT NOT NULL,
                last_accessed_at TEXT NOT NULL
            )
            """;

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        _initialized = true;
    }

    private static byte[] SerializeEmbedding(ReadOnlyMemory<float> embedding)
    {
        var span = embedding.Span;
        var bytes = new byte[span.Length * sizeof(float)];
        Buffer.BlockCopy(span.ToArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static ReadOnlyMemory<float> DeserializeEmbedding(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

    private static string SerializeMetadata(IDictionary<string, string> metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return "{}";
        }

        // Simple key=value serialization (avoid System.Text.Json dependency for netstandard2.0)
        var pairs = metadata.Select(kv =>
            $"\"{EscapeJsonString(kv.Key)}\":\"{EscapeJsonString(kv.Value)}\"");
        return "{" + string.Join(",", pairs) + "}";
    }

    private static Dictionary<string, string> DeserializeMetadata(string json)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(json) || string.Equals(json, "{}", StringComparison.Ordinal))
        {
            return result;
        }

        // Simple JSON object parser for key-value string pairs
        var content = json.Trim().TrimStart('{').TrimEnd('}');
        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        foreach (var pair in SplitJsonPairs(content))
        {
            var colonIdx = pair.IndexOf(':');
            if (colonIdx <= 0)
            {
                continue;
            }

            var key = pair.Substring(0, colonIdx).Trim().Trim('"');
            var value = pair.Substring(colonIdx + 1).Trim().Trim('"');
            result[UnescapeJsonString(key)] = UnescapeJsonString(value);
        }

        return result;
    }

    private static IEnumerable<string> SplitJsonPairs(string content)
    {
        var depth = 0;
        var inString = false;
        var start = 0;
        var escaped = false;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (c == '{' || c == '[')
            {
                depth++;
            }
            else if (c == '}' || c == ']')
            {
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                yield return content.Substring(start, i - start);
                start = i + 1;
            }
        }

        if (start < content.Length)
        {
            yield return content.Substring(start);
        }
    }

#pragma warning disable MA0023 // Use string.Replace with StringComparison — not available on netstandard2.0
    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string UnescapeJsonString(string value)
    {
        return value
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\")
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t");
    }
#pragma warning restore MA0023

    private static double CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var spanA = a.Span;
        var spanB = b.Span;

        if (spanA.Length != spanB.Length || spanA.Length == 0)
        {
            return 0.0;
        }

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < spanA.Length; i++)
        {
            dot += spanA[i] * spanB[i];
            magA += spanA[i] * spanA[i];
            magB += spanB[i] * spanB[i];
        }

        var magnitude = Math.Sqrt(magA) * Math.Sqrt(magB);
        return magnitude > 0 ? dot / magnitude : 0.0;
    }
}
