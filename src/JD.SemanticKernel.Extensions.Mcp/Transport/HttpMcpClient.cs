using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JD.SemanticKernel.Extensions.Mcp.Transport;

/// <summary>
/// MCP client that communicates with a server over HTTP using JSON-RPC 2.0.
/// </summary>
public sealed class HttpMcpClient : IMcpClient, IDisposable
{
    private readonly Uri _endpoint;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    // Serializes concurrent InitializeAsync calls so the handshake runs exactly once.
    private readonly SemaphoreSlim _initializeLock = new SemaphoreSlim(1, 1);
    private int _nextId;
    private volatile bool _initialized;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpMcpClient"/> using a provided <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="endpoint">The JSON-RPC endpoint URI of the MCP server.</param>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    public HttpMcpClient(Uri endpoint, HttpClient httpClient)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(httpClient);
#else
        if (endpoint is null) throw new ArgumentNullException(nameof(endpoint));
        if (httpClient is null) throw new ArgumentNullException(nameof(httpClient));
#endif
        _endpoint = endpoint;
        _httpClient = httpClient;
        _ownsHttpClient = false;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="HttpMcpClient"/> that creates and owns its own <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="endpoint">The JSON-RPC endpoint URI of the MCP server.</param>
    public HttpMcpClient(Uri endpoint)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(endpoint);
#else
        if (endpoint is null) throw new ArgumentNullException(nameof(endpoint));
#endif
        _endpoint = endpoint;
        _httpClient = new HttpClient();
        _ownsHttpClient = true;
    }

    /// <summary>
    /// Creates an <see cref="HttpMcpClient"/> from a <see cref="McpServerDefinition"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the definition does not use HTTP transport.</exception>
    public static HttpMcpClient FromDefinition(McpServerDefinition definition)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(definition);
#else
        if (definition is null) throw new ArgumentNullException(nameof(definition));
#endif

        if (definition.Transport != McpTransportType.Http || definition.Url is null)
            throw new ArgumentException("Definition must use HTTP transport and have a URL.", nameof(definition));

        return new HttpMcpClient(definition.Url);
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        await _initializeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring the lock to handle concurrent callers.
            if (_initialized)
                return;

            var requestId = Interlocked.Increment(ref _nextId);
            var request = CreateRequest(requestId, "initialize", new
            {
                protocolVersion = "2024-11-05",
                clientInfo = new { name = "JD.SemanticKernel.Extensions.Mcp", version = "1.0" },
                capabilities = new { }
            });

            using var response = await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);

            // Validate that the initialize response is not a JSON-RPC error before proceeding.
            if (response.RootElement.TryGetProperty("error", out var initErrorEl))
                throw new InvalidOperationException($"MCP initialize failed: {initErrorEl.GetRawText()}");

            // Send the follow-up notifications/initialized notification expected by some servers.
            await SendNotificationAsync("notifications/initialized", cancellationToken).ConfigureAwait(false);

            _initialized = true;
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<McpToolDefinition>> GetToolsAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var requestId = Interlocked.Increment(ref _nextId);
        var request = CreateRequest(requestId, "tools/list", new { });

        using var response = await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        return McpResponseParser.ParseTools(response);
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
        var request = CreateRequest(requestId, "tools/call", new
        {
            name = toolName,
            arguments
        });

        using var response = await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        return McpResponseParser.ParseInvocationResult(response);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _initializeLock.Dispose();
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }

    private static object CreateRequest(int id, string method, object @params) =>
        new { jsonrpc = "2.0", id, method, @params };

    private async Task SendNotificationAsync(string method, CancellationToken cancellationToken)
    {
        var notification = new { jsonrpc = "2.0", method };
        var json = JsonSerializer.Serialize(notification);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = content
        };
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var httpResponse = await _httpClient
            .SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);

        // Notifications/initialized should succeed; a failure indicates a server-side rejection.
        httpResponse.EnsureSuccessStatusCode();
    }

    private async Task<JsonDocument> SendRequestAsync(object request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = content
        };
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var httpResponse = await _httpClient
            .SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);

        httpResponse.EnsureSuccessStatusCode();

        var responseJson = await httpResponse.Content
            .ReadAsStringAsync(
#if NET8_0_OR_GREATER
            cancellationToken
#endif
            )
            .ConfigureAwait(false);

        return JsonDocument.Parse(responseJson);
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("InitializeAsync must be called before using this client.");
    }
}
