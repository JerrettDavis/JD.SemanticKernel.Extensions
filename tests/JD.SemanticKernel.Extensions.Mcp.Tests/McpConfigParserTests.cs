using JD.SemanticKernel.Extensions.Mcp;
using JD.SemanticKernel.Extensions.Mcp.Discovery;

namespace JD.SemanticKernel.Extensions.Mcp.Tests;

public class McpConfigParserTests
{
    [Fact]
    public void ParseMcpServers_StdioServer_ReturnsDefinition()
    {
        var json = """
            {
                "mcpServers": {
                    "notion": {
                        "command": "npx",
                        "args": ["-y", "@notion-mcp/server"]
                    }
                }
            }
            """;

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var results = McpConfigParser.ParseMcpServers(
            doc.RootElement, "test-provider", "/path/config.json", McpScope.User);

        Assert.Single(results);
        var def = results[0];
        Assert.Equal("notion", def.Name);
        Assert.Equal(McpTransportType.Stdio, def.Transport);
        Assert.Equal("npx", def.Command);
        Assert.Equal(2, def.Args!.Count);
        Assert.Equal("-y", def.Args[0]);
        Assert.Equal("@notion-mcp/server", def.Args[1]);
        Assert.True(def.IsEnabled);
    }

    [Fact]
    public void ParseMcpServers_HttpServer_ReturnsDefinitionWithUrl()
    {
        var json = """
            {
                "mcpServers": {
                    "remote-tools": {
                        "url": "http://localhost:8080/mcp"
                    }
                }
            }
            """;

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var results = McpConfigParser.ParseMcpServers(
            doc.RootElement, "test-provider", "/path/config.json", McpScope.User);

        Assert.Single(results);
        var def = results[0];
        Assert.Equal("remote-tools", def.Name);
        Assert.Equal(McpTransportType.Http, def.Transport);
        Assert.NotNull(def.Url);
        Assert.Equal("http://localhost:8080/mcp", def.Url!.ToString());
    }

    [Fact]
    public void ParseMcpServers_DisabledServer_IsEnabledFalse()
    {
        var json = """
            {
                "mcpServers": {
                    "disabled-tool": {
                        "command": "echo",
                        "disabled": true
                    }
                }
            }
            """;

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var results = McpConfigParser.ParseMcpServers(
            doc.RootElement, "test-provider", "/path/config.json", McpScope.User);

        Assert.Single(results);
        Assert.False(results[0].IsEnabled);
    }

    [Fact]
    public void ParseMcpServers_WithEnvVars_SetsEnv()
    {
        var json = """
            {
                "mcpServers": {
                    "env-server": {
                        "command": "node",
                        "env": {
                            "API_KEY": "secret",
                            "REGION": "us-east-1"
                        }
                    }
                }
            }
            """;

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var results = McpConfigParser.ParseMcpServers(
            doc.RootElement, "test-provider", "/path/config.json", McpScope.User);

        Assert.Single(results);
        var def = results[0];
        Assert.NotNull(def.Env);
        Assert.Equal("secret", def.Env!["API_KEY"]);
        Assert.Equal("us-east-1", def.Env["REGION"]);
    }

    [Fact]
    public void ParseMcpServers_NoMcpServersKey_ReturnsEmpty()
    {
        var json = """{ "other": {} }""";

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var results = McpConfigParser.ParseMcpServers(
            doc.RootElement, "test-provider", "/path/config.json", McpScope.User);

        Assert.Empty(results);
    }

    [Fact]
    public void ParseMcpServers_MultipleServers_ReturnsAll()
    {
        var json = """
            {
                "mcpServers": {
                    "server-a": { "command": "cmd-a" },
                    "server-b": { "command": "cmd-b" }
                }
            }
            """;

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var results = McpConfigParser.ParseMcpServers(
            doc.RootElement, "test-provider", "/path/config.json", McpScope.User);

        Assert.Equal(2, results.Count);
    }
}
