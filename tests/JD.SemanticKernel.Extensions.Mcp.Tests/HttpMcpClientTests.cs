using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JD.SemanticKernel.Extensions.Mcp.Transport;

namespace JD.SemanticKernel.Extensions.Mcp.Tests;

/// <summary>
/// Unit tests for <see cref="HttpMcpClient"/> using a stub <see cref="HttpMessageHandler"/>.
/// </summary>
public class HttpMcpClientTests
{
    private static readonly string[] RequiredMessageField = ["message"];

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string BuildInitializeResponse(int id = 1) => JsonSerializer.Serialize(new
    {
        jsonrpc = "2.0",
        id,
        result = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { },
            serverInfo = new { name = "TestServer", version = "1.0" }
        }
    });

    private static string BuildToolListResponse(int id = 2) => JsonSerializer.Serialize(new
    {
        jsonrpc = "2.0",
        id,
        result = new
        {
            tools = new[]
            {
                new
                {
                    name = "echo",
                    description = "Echoes the input",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            message = new { type = "string", description = "The message to echo" }
                        },
                        required = RequiredMessageField
                    }
                }
            }
        }
    });

    private static string BuildInvokeResponse(int id = 3, string text = "hello") =>
        JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id,
            result = new { content = new[] { new { type = "text", text } } }
        });

    private static string BuildErrorResponse(int id, string message) =>
        JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id,
            error = new { code = -32000, message }
        });

    // ── InitializeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_Succeeds_WhenServerRespondsWithoutError()
    {
        var callCount = 0;
        var handler = new StubHandler(request =>
        {
            callCount++;
            if (callCount == 1)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildInitializeResponse(), Encoding.UTF8, "application/json")
                };

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        using var http = new HttpClient(handler);
        using var client = new HttpMcpClient(new Uri("http://localhost/mcp"), http);

        await client.InitializeAsync();

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task InitializeAsync_ThrowsOnJsonRpcError()
    {
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    BuildErrorResponse(1, "unsupported version"), Encoding.UTF8, "application/json")
            });

        using var http = new HttpClient(handler);
        using var client = new HttpMcpClient(new Uri("http://localhost/mcp"), http);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.InitializeAsync());
        Assert.Contains("MCP initialize failed", ex.Message);
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent_WhenCalledConcurrently()
    {
        var callCount = 0;
        var handler = new StubHandler(_ =>
        {
            Interlocked.Increment(ref callCount);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildInitializeResponse(), Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new HttpMcpClient(new Uri("http://localhost/mcp"), http);

        await Task.WhenAll(client.InitializeAsync(), client.InitializeAsync());

        // 1 initialize + 1 notifications/initialized = 2 calls total
        Assert.Equal(2, callCount);
    }

    // ── GetToolsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetToolsAsync_ReturnsToolsFromResponse()
    {
        var seq = new Queue<string>(new[]
        {
            BuildInitializeResponse(1),
            string.Empty,
            BuildToolListResponse(2)
        });

        var handler = new StubHandler(_ =>
        {
            var body = seq.Count > 0 ? seq.Dequeue() : "{}";
            if (string.IsNullOrEmpty(body))
                return new HttpResponseMessage(HttpStatusCode.NoContent);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new HttpMcpClient(new Uri("http://localhost/mcp"), http);

        await client.InitializeAsync();
        var tools = await client.GetToolsAsync();

        Assert.Single(tools);
        Assert.Equal("echo", tools[0].Name);
        Assert.Equal("Echoes the input", tools[0].Description);
        var param = Assert.Single(tools[0].Parameters);
        Assert.Equal("message", param.Name);
        Assert.Equal("string", param.Type);
        Assert.True(param.IsRequired);
    }

    [Fact]
    public async Task GetToolsAsync_ThrowsWhenNotInitialized()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        using var http = new HttpClient(handler);
        using var client = new HttpMcpClient(new Uri("http://localhost/mcp"), http);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetToolsAsync());
    }

    // ── InvokeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_ReturnsSuccessContent()
    {
        var seq = new Queue<string>(new[]
        {
            BuildInitializeResponse(1),
            string.Empty,
            BuildInvokeResponse(2, "hello world")
        });

        var handler = new StubHandler(_ =>
        {
            var body = seq.Count > 0 ? seq.Dequeue() : "{}";
            if (string.IsNullOrEmpty(body))
                return new HttpResponseMessage(HttpStatusCode.NoContent);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new HttpMcpClient(new Uri("http://localhost/mcp"), http);

        await client.InitializeAsync();
        var result = await client.InvokeAsync("echo",
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["message"] = "hello world" });

        Assert.False(result.IsError);
        Assert.Equal("hello world", result.Content);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsFailureOnJsonRpcError()
    {
        var seq = new Queue<string>(new[]
        {
            BuildInitializeResponse(1),
            string.Empty,
            BuildErrorResponse(2, "tool not found")
        });

        var handler = new StubHandler(_ =>
        {
            var body = seq.Count > 0 ? seq.Dequeue() : "{}";
            if (string.IsNullOrEmpty(body))
                return new HttpResponseMessage(HttpStatusCode.NoContent);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new HttpMcpClient(new Uri("http://localhost/mcp"), http);

        await client.InitializeAsync();
        var result = await client.InvokeAsync("missing",
            new Dictionary<string, object?>(StringComparer.Ordinal));

        Assert.True(result.IsError);
        Assert.Equal("tool not found", result.ErrorMessage);
    }

    // ── Stub ─────────────────────────────────────────────────────────────────

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
