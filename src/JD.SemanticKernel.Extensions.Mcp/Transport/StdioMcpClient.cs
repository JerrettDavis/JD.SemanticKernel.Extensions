using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JD.SemanticKernel.Extensions.Mcp.Transport;

/// <summary>
/// MCP client that communicates with a server via standard input/output (STDIO) using JSON-RPC.
/// </summary>
public sealed class StdioMcpClient : IMcpClient, IDisposable
{
    private readonly string _command;
    private readonly IReadOnlyList<string>? _args;
    private readonly IReadOnlyDictionary<string, string>? _env;

    // Serializes send+receive pairs so concurrent callers cannot interleave writes/reads.
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    private Process? _process;
    private int _nextId;
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of <see cref="StdioMcpClient"/>.
    /// </summary>
    /// <param name="command">The executable command to launch.</param>
    /// <param name="args">Optional arguments to pass to the command.</param>
    /// <param name="env">Optional environment variables to set.</param>
    public StdioMcpClient(
        string command,
        IReadOnlyList<string>? args = null,
        IReadOnlyDictionary<string, string>? env = null)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
#else
        if (string.IsNullOrWhiteSpace(command)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(command));
#endif
        _command = command;
        _args = args;
        _env = env;
    }

    /// <summary>
    /// Creates a <see cref="StdioMcpClient"/> from a <see cref="McpServerDefinition"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the definition does not use STDIO transport.</exception>
    public static StdioMcpClient FromDefinition(McpServerDefinition definition)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(definition);
#else
        if (definition is null) throw new ArgumentNullException(nameof(definition));
#endif

        if (definition.Transport != McpTransportType.Stdio || definition.Command is null)
            throw new ArgumentException("Definition must use STDIO transport and have a command.", nameof(definition));

        return new StdioMcpClient(definition.Command, definition.Args, definition.Env);
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring the lock to handle concurrent callers.
            if (_initialized)
                return;

            EnsureProcess();

            var requestId = Interlocked.Increment(ref _nextId);
            var initRequest = new
            {
                jsonrpc = "2.0",
                id = requestId,
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2024-11-05",
                    clientInfo = new { name = "JD.SemanticKernel.Extensions.Mcp", version = "1.0" },
                    capabilities = new { }
                }
            };

            await WriteLineAsync(JsonSerializer.Serialize(initRequest), cancellationToken).ConfigureAwait(false);

            using var response = await ReadResponseCoreAsync(cancellationToken).ConfigureAwait(false);

            // Validate the initialize response is not a JSON-RPC error.
            if (response.RootElement.TryGetProperty("error", out var initErrorEl))
                throw new InvalidOperationException($"MCP initialize failed: {initErrorEl.GetRawText()}");

            // Send the initialized notification expected by the server.
            var notification = new { jsonrpc = "2.0", method = "notifications/initialized" };
            await WriteLineAsync(JsonSerializer.Serialize(notification), cancellationToken).ConfigureAwait(false);

            _initialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<McpToolDefinition>> GetToolsAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var requestId = Interlocked.Increment(ref _nextId);
        var request = new
        {
            jsonrpc = "2.0",
            id = requestId,
            method = "tools/list",
            @params = new { }
        };

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteLineAsync(JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);
            using var response = await ReadResponseCoreAsync(cancellationToken).ConfigureAwait(false);
            return ParseTools(response);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<McpInvocationResult> InvokeAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(arguments);
#else
        if (string.IsNullOrWhiteSpace(toolName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(toolName));
        if (arguments is null) throw new ArgumentNullException(nameof(arguments));
#endif

        EnsureInitialized();

        var requestId = Interlocked.Increment(ref _nextId);
        var request = new
        {
            jsonrpc = "2.0",
            id = requestId,
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments
            }
        };

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteLineAsync(JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);
            using var response = await ReadResponseCoreAsync(cancellationToken).ConfigureAwait(false);
            return ParseInvocationResult(response);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_process is not null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.StandardInput.Close();
                    _process.WaitForExit(2000);
                    if (!_process.HasExited)
                        _process.Kill();
                }
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                // Suppress errors during cleanup
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }

        _lock.Dispose();
    }

    private void EnsureProcess()
    {
        if (_process is not null)
        {
            if (!_process.HasExited)
                return;

            _process.Dispose();
            _process = null;
        }

        var psi = new ProcessStartInfo
        {
            FileName = _command,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            // Do NOT redirect stderr: if the server writes enough stderr output the pipe buffer
            // fills and blocks the server process, causing tool calls to hang.
            UseShellExecute = false,
            CreateNoWindow = true,
#if NET8_0_OR_GREATER
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
#endif
        };

        if (_args is not null)
        {
#if NET8_0_OR_GREATER
            foreach (var arg in _args)
                psi.ArgumentList.Add(arg);
#else
            // netstandard2.0: build a properly quoted argument string.
            var parts = new List<string>(_args.Count);
            foreach (var arg in _args)
                parts.Add(QuoteArgument(arg));
            psi.Arguments = string.Join(" ", parts);
#endif
        }

        if (_env is not null)
        {
            foreach (var kvp in _env)
                psi.Environment[kvp.Key] = kvp.Value;
        }

        _process = new Process { StartInfo = psi };
        _process.Start();
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("InitializeAsync must be called before using this client.");
    }

    private async Task WriteLineAsync(string line, CancellationToken cancellationToken)
    {
        await _process!.StandardInput.WriteLineAsync(line
#if NET8_0_OR_GREATER
            .AsMemory(), cancellationToken
#endif
        ).ConfigureAwait(false);
        await _process.StandardInput.FlushAsync(
#if NET8_0_OR_GREATER
            cancellationToken
#endif
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the next JSON-RPC response line, skipping notifications (messages without an <c>id</c>)
    /// and malformed lines. The caller is responsible for disposing the returned <see cref="JsonDocument"/>.
    /// </summary>
    private async Task<JsonDocument> ReadResponseCoreAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

#if NET8_0_OR_GREATER
            var line = await _process!.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#else
            var line = await _process!.StandardOutput.ReadLineAsync().ConfigureAwait(false);
#endif
            if (line is null)
                throw new InvalidOperationException("MCP server closed the output stream unexpectedly.");

            if (string.IsNullOrWhiteSpace(line))
                continue;

            JsonDocument? document = null;
            try
            {
                document = JsonDocument.Parse(line);
            }
#pragma warning disable CA1031 // Malformed JSON lines must not crash the read loop
            catch (Exception)
#pragma warning restore CA1031
            {
                // Ignore malformed JSON lines and continue reading.
                continue;
            }

            // JSON-RPC notifications do not include an "id" field; skip them.
            if (!document.RootElement.TryGetProperty("id", out var idProperty) ||
                idProperty.ValueKind == JsonValueKind.Null)
            {
                document.Dispose();
                continue;
            }

            return document;
        }
    }

    private static List<McpToolDefinition> ParseTools(JsonDocument response)
    {
        var results = new List<McpToolDefinition>();

        if (!response.RootElement.TryGetProperty("result", out var result))
            return results;

        if (!result.TryGetProperty("tools", out var toolsEl) ||
            toolsEl.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var toolEl in toolsEl.EnumerateArray())
        {
            if (!toolEl.TryGetProperty("name", out var nameEl) ||
                nameEl.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var name = nameEl.GetString()!;
            string? description = null;
            if (toolEl.TryGetProperty("description", out var descEl) &&
                descEl.ValueKind == JsonValueKind.String)
            {
                description = descEl.GetString();
            }

            var parameters = ParseToolParameters(toolEl);
            results.Add(new McpToolDefinition(name, description, parameters));
        }

        return results;
    }

    private static List<McpToolParameter> ParseToolParameters(JsonElement toolEl)
    {
        var results = new List<McpToolParameter>();

        if (!toolEl.TryGetProperty("inputSchema", out var schemaEl) ||
            schemaEl.ValueKind != JsonValueKind.Object)
        {
            return results;
        }

        if (!schemaEl.TryGetProperty("properties", out var propsEl) ||
            propsEl.ValueKind != JsonValueKind.Object)
        {
            return results;
        }

        var requiredNames = new HashSet<string>(StringComparer.Ordinal);
        if (schemaEl.TryGetProperty("required", out var reqEl) &&
            reqEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var req in reqEl.EnumerateArray())
            {
                if (req.ValueKind == JsonValueKind.String)
                    requiredNames.Add(req.GetString()!);
            }
        }

        foreach (var prop in propsEl.EnumerateObject())
        {
            string? description = null;
            string? type = null;

            if (prop.Value.TryGetProperty("description", out var descEl) &&
                descEl.ValueKind == JsonValueKind.String)
            {
                description = descEl.GetString();
            }

            if (prop.Value.TryGetProperty("type", out var typeEl) &&
                typeEl.ValueKind == JsonValueKind.String)
            {
                type = typeEl.GetString();
            }

            results.Add(new McpToolParameter(
                prop.Name,
                description,
                type,
                requiredNames.Contains(prop.Name)));
        }

        return results;
    }

    private static McpInvocationResult ParseInvocationResult(JsonDocument response)
    {
        var root = response.RootElement;

        if (root.TryGetProperty("error", out var errorEl))
        {
            var message = errorEl.TryGetProperty("message", out var msgEl)
                ? msgEl.GetString() ?? "Unknown error"
                : "Unknown error";
            return McpInvocationResult.Failure(message);
        }

        if (!root.TryGetProperty("result", out var result))
            return McpInvocationResult.Success(null);

        // MCP result may contain an array of content items
        if (result.TryGetProperty("content", out var contentEl) &&
            contentEl.ValueKind == JsonValueKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var item in contentEl.EnumerateArray())
            {
                if (item.TryGetProperty("text", out var textEl) &&
                    textEl.ValueKind == JsonValueKind.String)
                {
                    sb.Append(textEl.GetString());
                }
            }

            return McpInvocationResult.Success(sb.Length > 0 ? sb.ToString() : null);
        }

        return McpInvocationResult.Success(result.GetRawText());
    }

    /// <summary>
    /// Quotes a single command-line argument for the netstandard2.0 <c>ProcessStartInfo.Arguments</c> path.
    /// Arguments containing spaces, tabs, or double-quotes are wrapped in double-quotes with inner
    /// double-quotes escaped.
    /// </summary>
    private static string QuoteArgument(string arg)
    {
        if (arg.Length == 0)
            return "\"\"";

        var needsQuoting = false;
        foreach (var c in arg)
        {
            if (c == ' ' || c == '\t' || c == '"')
            {
                needsQuoting = true;
                break;
            }
        }

        if (!needsQuoting)
            return arg;

        return '"' + arg.Replace("\"", "\\\"") + '"';
    }
}
