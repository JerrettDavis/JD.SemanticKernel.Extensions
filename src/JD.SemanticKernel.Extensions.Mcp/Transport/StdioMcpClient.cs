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
    private readonly string? _command;
    private readonly IReadOnlyList<string>? _args;
    private readonly IReadOnlyDictionary<string, string>? _env;

    // Overrides for testing: when set, bypass the process and use these streams directly.
    // These are caller-owned; the client must NOT dispose them.
    // Must be StreamReader/StreamWriter to support cancellation-token overloads on NET8+.
#pragma warning disable CA2213 // Caller owns these streams and is responsible for disposal
    private readonly StreamReader? _injectedReader;
    private readonly StreamWriter? _injectedWriter;
#pragma warning restore CA2213

    // Serializes send+receive pairs so concurrent callers cannot interleave writes/reads.
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    private Process? _process;
    private int _nextId;
    private volatile bool _initialized;

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

    /// <summary>
    /// Creates a <see cref="StdioMcpClient"/> that reads from and writes to the provided streams
    /// without launching a process. Intended for unit testing only.
    /// </summary>
    internal static StdioMcpClient CreateForTesting(StreamReader reader, StreamWriter writer)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(writer);
#else
        if (reader is null) throw new ArgumentNullException(nameof(reader));
        if (writer is null) throw new ArgumentNullException(nameof(writer));
#endif
        return new StdioMcpClient(reader, writer);
    }

    /// <summary>Testing constructor: uses injected streams instead of a process.</summary>
    private StdioMcpClient(StreamReader reader, StreamWriter writer)
    {
        _command = null;
        _injectedReader = reader;
        _injectedWriter = writer;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: already initialized and either using injected test streams or process is
        // still alive.  If the server process has exited, fall through so we re-handshake.
        if (_initialized && (_injectedReader != null || (_process != null && !_process.HasExited)))
            return;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // If the server process exited after the last successful handshake, reset the
            // initialized flag so the full handshake runs for the new process.
            if (_injectedReader == null && _process?.HasExited == true)
                _initialized = false;

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
            return McpResponseParser.ParseTools(response);
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
            return McpResponseParser.ParseInvocationResult(response);
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
        // When using injected streams (for testing), no process is needed.
        if (_injectedReader is not null)
            return;

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
        var writer = _injectedWriter ?? _process!.StandardInput;
        await writer.WriteLineAsync(line
#if NET8_0_OR_GREATER
            .AsMemory(), cancellationToken
#endif
        ).ConfigureAwait(false);
        await writer.FlushAsync(
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
        var reader = _injectedReader ?? _process!.StandardOutput;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

#if NET8_0_OR_GREATER
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#else
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
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

    /// <summary>
    /// Quotes a single command-line argument for the netstandard2.0 <c>ProcessStartInfo.Arguments</c> path,
    /// following the Windows CRT / <c>CommandLineToArgvW</c> quoting rules:
    /// <list type="bullet">
    ///   <item>N backslashes immediately before a <c>"</c> → 2*N backslashes + <c>\"</c></item>
    ///   <item>N trailing backslashes (at end of argument, before the closing <c>"</c>) → 2*N backslashes</item>
    ///   <item>All other backslashes are passed through unchanged.</item>
    /// </list>
    /// Arguments that contain no spaces, tabs, or double-quotes are returned as-is.
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

        // Implement Windows CRT / CommandLineToArgvW quoting rules.
        var sb = new StringBuilder(arg.Length + 2);
        sb.Append('"');
        var backslashCount = 0;
        foreach (var c in arg)
        {
            if (c == '\\')
            {
                backslashCount++;
            }
            else if (c == '"')
            {
                // Double the preceding backslashes, then emit an escaped quote.
                sb.Append('\\', backslashCount * 2);
                backslashCount = 0;
                sb.Append('\\');
                sb.Append('"');
            }
            else
            {
                // Non-special character: flush pending backslashes as-is.
                sb.Append('\\', backslashCount);
                backslashCount = 0;
                sb.Append(c);
            }
        }

        // Double any trailing backslashes so they don't accidentally escape the closing '"'.
        sb.Append('\\', backslashCount * 2);
        sb.Append('"');
        return sb.ToString();
    }
}
