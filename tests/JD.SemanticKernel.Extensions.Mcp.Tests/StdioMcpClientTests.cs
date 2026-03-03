using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JD.SemanticKernel.Extensions.Mcp.Transport;

namespace JD.SemanticKernel.Extensions.Mcp.Tests;

/// <summary>
/// Unit tests for <see cref="StdioMcpClient"/> using an in-process fake MCP server over pipes.
/// </summary>
public class StdioMcpClientTests
{
    private static readonly string[] RequiredNameField = ["name"];

    // ── fake server helpers ───────────────────────────────────────────────────

    private static async Task RunFakeServerAsync(
        StreamReader serverIn,
        StreamWriter serverOut,
        Func<JsonDocument, string?> responseFactory,
        int maxRequests,
        CancellationToken ct = default)
    {
        var count = 0;
        while (!ct.IsCancellationRequested && count < maxRequests)
        {
            var line = await serverIn.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            using var doc = JsonDocument.Parse(line);
            var response = responseFactory(doc);
            if (response is not null)
            {
                await serverOut.WriteLineAsync(response.AsMemory(), ct).ConfigureAwait(false);
                await serverOut.FlushAsync(ct).ConfigureAwait(false);
            }

            count++;
        }
    }

    private static (StreamReader clientIn, StreamWriter clientOut,
                    StreamReader serverIn, StreamWriter serverOut)
        CreatePipePair()
    {
        var clientToServer = new System.IO.Pipes.AnonymousPipeServerStream(
            System.IO.Pipes.PipeDirection.In,
            System.IO.HandleInheritability.None);
        var clientToServerClient = new System.IO.Pipes.AnonymousPipeClientStream(
            System.IO.Pipes.PipeDirection.Out,
            clientToServer.GetClientHandleAsString());
        // Note: DisposeLocalCopyOfClientHandle() is intentionally NOT called here.
        // In cross-process usage the server would call it to release its duplicate of
        // the inherited client handle so EOF is detected when the child process closes
        // its copy.  In this in-process test, AnonymousPipeClientStream takes ownership
        // of the same OS handle (ownsHandle: true); calling DisposeLocalCopyOfClientHandle
        // would close the only OS handle, invalidating the client stream.

        var serverToClient = new System.IO.Pipes.AnonymousPipeServerStream(
            System.IO.Pipes.PipeDirection.Out,
            System.IO.HandleInheritability.None);
        var serverToClientClient = new System.IO.Pipes.AnonymousPipeClientStream(
            System.IO.Pipes.PipeDirection.In,
            serverToClient.GetClientHandleAsString());

        // The StreamReader/StreamWriter wrappers below use leaveOpen: false (the default),
        // so disposing them disposes the underlying pipe streams, releasing all OS handles.
        var serverReader = new StreamReader(clientToServer, Encoding.UTF8);
        var serverWriter = new StreamWriter(serverToClient, Encoding.UTF8) { AutoFlush = true };
        var clientReader = new StreamReader(serverToClientClient, Encoding.UTF8);
        var clientWriter = new StreamWriter(clientToServerClient, Encoding.UTF8) { AutoFlush = true };

        return (clientReader, clientWriter, serverReader, serverWriter);
    }

    // ── response builders ─────────────────────────────────────────────────────

    private static string InitializeResponse(int id) => JsonSerializer.Serialize(new
    {
        jsonrpc = "2.0",
        id,
        result = new { protocolVersion = "2024-11-05", capabilities = new { } }
    });

    private static string ToolListResponse(int id) => JsonSerializer.Serialize(new
    {
        jsonrpc = "2.0",
        id,
        result = new
        {
            tools = new[]
            {
                new
                {
                    name = "greet",
                    description = "Returns a greeting",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string", description = "Name to greet" }
                        },
                        required = RequiredNameField
                    }
                }
            }
        }
    });

    private static string InvokeResponse(int id, string text) => JsonSerializer.Serialize(new
    {
        jsonrpc = "2.0",
        id,
        result = new { content = new[] { new { type = "text", text } } }
    });

    private static string ErrorResponse(int id, string message) => JsonSerializer.Serialize(new
    {
        jsonrpc = "2.0",
        id,
        error = new { code = -32000, message }
    });

    private static string Notification(string method) => JsonSerializer.Serialize(new
    {
        jsonrpc = "2.0",
        method
    });

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_HandshakeSucceeds()
    {
        var (clientReader, clientWriter, serverReader, serverWriter) = CreatePipePair();

        var serverTask = RunFakeServerAsync(serverReader, serverWriter, doc =>
        {
            var method = doc.RootElement.GetProperty("method").GetString();
            if (string.Equals(method, "initialize", StringComparison.Ordinal))
                return InitializeResponse(doc.RootElement.GetProperty("id").GetInt32());
            return null;
        }, maxRequests: 2);

        using var client = StdioMcpClient.CreateForTesting(clientReader, clientWriter);
        await client.InitializeAsync();

        clientWriter.BaseStream.Close();
        await serverTask;
    }

    [Fact]
    public async Task InitializeAsync_ThrowsWhenServerReturnsError()
    {
        var (clientReader, clientWriter, serverReader, serverWriter) = CreatePipePair();

        var serverTask = RunFakeServerAsync(serverReader, serverWriter, doc =>
        {
            var id = doc.RootElement.GetProperty("id").GetInt32();
            return ErrorResponse(id, "version not supported");
        }, maxRequests: 1);

        using var client = StdioMcpClient.CreateForTesting(clientReader, clientWriter);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.InitializeAsync());
        Assert.Contains("MCP initialize failed", ex.Message);

        clientWriter.BaseStream.Close();
        await serverTask;
    }

    [Fact]
    public async Task InitializeAsync_SkipsNotifications()
    {
        var (clientReader, clientWriter, serverReader, serverWriter) = CreatePipePair();

        var serverTask = Task.Run(async () =>
        {
            var line = await serverReader.ReadLineAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(line!);
            var id = doc.RootElement.GetProperty("id").GetInt32();

            // Emit a notification BEFORE the response
            await serverWriter.WriteLineAsync(Notification("$/progress")).ConfigureAwait(false);
            await serverWriter.WriteLineAsync(InitializeResponse(id)).ConfigureAwait(false);
            await serverWriter.FlushAsync().ConfigureAwait(false);

            // Consume notifications/initialized
            await serverReader.ReadLineAsync().ConfigureAwait(false);
        });

        using var client = StdioMcpClient.CreateForTesting(clientReader, clientWriter);
        await client.InitializeAsync();

        clientWriter.BaseStream.Close();
        await serverTask;
    }

    [Fact]
    public async Task GetToolsAsync_ReturnsParsedTools()
    {
        var (clientReader, clientWriter, serverReader, serverWriter) = CreatePipePair();

        var serverTask = RunFakeServerAsync(serverReader, serverWriter, doc =>
        {
            var method = doc.RootElement.GetProperty("method").GetString();
            var id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
            return method switch
            {
                "initialize" => InitializeResponse(id),
                "tools/list" => ToolListResponse(id),
                _ => null
            };
        }, maxRequests: 3);

        using var client = StdioMcpClient.CreateForTesting(clientReader, clientWriter);
        await client.InitializeAsync();
        var tools = await client.GetToolsAsync();

        Assert.Single(tools);
        Assert.Equal("greet", tools[0].Name);
        var param = Assert.Single(tools[0].Parameters);
        Assert.Equal("name", param.Name);
        Assert.True(param.IsRequired);

        clientWriter.BaseStream.Close();
        await serverTask;
    }

    [Fact]
    public async Task InvokeAsync_ReturnsSuccessContent()
    {
        var (clientReader, clientWriter, serverReader, serverWriter) = CreatePipePair();

        var serverTask = RunFakeServerAsync(serverReader, serverWriter, doc =>
        {
            var method = doc.RootElement.GetProperty("method").GetString();
            var id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
            return method switch
            {
                "initialize" => InitializeResponse(id),
                "tools/call" => InvokeResponse(id, "Hello, World!"),
                _ => null
            };
        }, maxRequests: 3);

        using var client = StdioMcpClient.CreateForTesting(clientReader, clientWriter);
        await client.InitializeAsync();
        var result = await client.InvokeAsync("greet",
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "World" });

        Assert.False(result.IsError);
        Assert.Equal("Hello, World!", result.Content);

        clientWriter.BaseStream.Close();
        await serverTask;
    }

    [Fact]
    public async Task InvokeAsync_ReturnsFailureOnJsonRpcError()
    {
        var (clientReader, clientWriter, serverReader, serverWriter) = CreatePipePair();

        var serverTask = RunFakeServerAsync(serverReader, serverWriter, doc =>
        {
            var method = doc.RootElement.GetProperty("method").GetString();
            var id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
            return method switch
            {
                "initialize" => InitializeResponse(id),
                "tools/call" => ErrorResponse(id, "tool execution failed"),
                _ => null
            };
        }, maxRequests: 3);

        using var client = StdioMcpClient.CreateForTesting(clientReader, clientWriter);
        await client.InitializeAsync();
        var result = await client.InvokeAsync("greet",
            new Dictionary<string, object?>(StringComparer.Ordinal));

        Assert.True(result.IsError);
        Assert.Equal("tool execution failed", result.ErrorMessage);

        clientWriter.BaseStream.Close();
        await serverTask;
    }

    [Fact]
    public async Task ReadResponseCoreAsync_IgnoresMalformedJsonLines()
    {
        var (clientReader, clientWriter, serverReader, serverWriter) = CreatePipePair();

        var serverTask = Task.Run(async () =>
        {
            var line = await serverReader.ReadLineAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(line!);
            var id = doc.RootElement.GetProperty("id").GetInt32();

            // Send a malformed line first, then the real response
            await serverWriter.WriteLineAsync("this is not json").ConfigureAwait(false);
            await serverWriter.WriteLineAsync(InitializeResponse(id)).ConfigureAwait(false);
            await serverWriter.FlushAsync().ConfigureAwait(false);

            // Consume notifications/initialized
            await serverReader.ReadLineAsync().ConfigureAwait(false);
        });

        using var client = StdioMcpClient.CreateForTesting(clientReader, clientWriter);
        await client.InitializeAsync();

        clientWriter.BaseStream.Close();
        await serverTask;
    }
}
