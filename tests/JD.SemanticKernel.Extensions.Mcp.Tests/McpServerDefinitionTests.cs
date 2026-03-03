using System;
using JD.SemanticKernel.Extensions.Mcp;

namespace JD.SemanticKernel.Extensions.Mcp.Tests;

public class McpServerDefinitionTests
{
    [Fact]
    public void Constructor_ValidStdioServer_SetsAllProperties()
    {
        var args = new[] { "--flag", "value" };
        var env = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal) { ["KEY"] = "VAL" };

        var def = new McpServerDefinition(
            name: "my-server",
            displayName: "My Server",
            transport: McpTransportType.Stdio,
            scope: McpScope.Project,
            sourceProvider: "jd-canonical",
            sourcePath: "/path/jdai.mcp.json",
            command: "node",
            args: args,
            env: env,
            isEnabled: true);

        Assert.Equal("my-server", def.Name);
        Assert.Equal("My Server", def.DisplayName);
        Assert.Equal(McpTransportType.Stdio, def.Transport);
        Assert.Equal(McpScope.Project, def.Scope);
        Assert.Equal("jd-canonical", def.SourceProvider);
        Assert.Equal("/path/jdai.mcp.json", def.SourcePath);
        Assert.Equal("node", def.Command);
        Assert.Equal(args, def.Args);
        Assert.Equal(env, def.Env);
        Assert.True(def.IsEnabled);
        Assert.Null(def.Url);
    }

    [Fact]
    public void Constructor_ValidHttpServer_SetsUrl()
    {
        var url = new Uri("http://localhost:3000");

        var def = new McpServerDefinition(
            name: "http-server",
            displayName: "HTTP Server",
            transport: McpTransportType.Http,
            scope: McpScope.User,
            sourceProvider: "vscode",
            url: url);

        Assert.Equal(url, def.Url);
        Assert.Equal(McpTransportType.Http, def.Transport);
        Assert.Null(def.Command);
    }

    [Fact]
    public void Constructor_DisabledServer_IsEnabledFalse()
    {
        var def = new McpServerDefinition(
            name: "disabled-server",
            displayName: "Disabled",
            transport: McpTransportType.Stdio,
            scope: McpScope.User,
            sourceProvider: "test",
            command: "echo",
            isEnabled: false);

        Assert.False(def.IsEnabled);
    }

    [Theory]
    [InlineData("", "display", "provider")]
    [InlineData("   ", "display", "provider")]
    [InlineData("name", "", "provider")]
    [InlineData("name", "display", "")]
    public void Constructor_InvalidArguments_ThrowsArgumentException(
        string name, string displayName, string sourceProvider)
    {
        Assert.Throws<ArgumentException>(() =>
            new McpServerDefinition(
                name: name,
                displayName: displayName,
                transport: McpTransportType.Stdio,
                scope: McpScope.User,
                sourceProvider: sourceProvider,
                command: "echo"));
    }
}
