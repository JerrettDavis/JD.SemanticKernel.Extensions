using System.IO;
using System.Threading.Tasks;
using JD.SemanticKernel.Extensions.Mcp;
using JD.SemanticKernel.Extensions.Mcp.Discovery;

namespace JD.SemanticKernel.Extensions.Mcp.Tests;

public class FileMcpDiscoveryProviderTests
{
    [Fact]
    public async Task ClaudeCodeProvider_NoConfigFiles_ReturnsEmpty()
    {
        var provider = new ClaudeCodeMcpDiscoveryProvider(workingDirectory: Path.GetTempPath());
        var results = await provider.DiscoverAsync();
        // May return entries from ~/.claude.json if present, but should not throw
        Assert.NotNull(results);
    }

    [Fact]
    public async Task ClaudeCodeProvider_ValidProjectConfig_ReturnsServers()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mcpJson = Path.Combine(tempDir, ".mcp.json");
            await File.WriteAllTextAsync(mcpJson, """
                {
                    "mcpServers": {
                        "test-tool": {
                            "command": "npx",
                            "args": ["@test/mcp-server"]
                        }
                    }
                }
                """);

            var provider = new ClaudeCodeMcpDiscoveryProvider(workingDirectory: tempDir);
            var results = await provider.DiscoverAsync();

            // Should include the project-level server
            var found = false;
            foreach (var s in results)
            {
                if (string.Equals(s.Name, "test-tool", System.StringComparison.Ordinal))
                {
                    found = true;
                    Assert.Equal(McpTransportType.Stdio, s.Transport);
                    Assert.Equal("npx", s.Command);
                    Assert.Equal(McpScope.Project, s.Scope);
                    Assert.Equal("claude-code", s.SourceProvider);
                }
            }

            Assert.True(found, "Expected 'test-tool' server to be discovered.");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task JdCanonicalProvider_ValidProjectConfig_ReturnsServers()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var configFile = Path.Combine(tempDir, "jdai.mcp.json");
            await File.WriteAllTextAsync(configFile, """
                {
                    "mcpServers": {
                        "jd-server": {
                            "command": "dotnet",
                            "args": ["run", "--project", "McpServer"]
                        }
                    }
                }
                """);

            var provider = new JdCanonicalMcpDiscoveryProvider(workingDirectory: tempDir);
            var results = await provider.DiscoverAsync();

            var found = false;
            foreach (var s in results)
            {
                if (string.Equals(s.Name, "jd-server", System.StringComparison.Ordinal))
                {
                    found = true;
                    Assert.Equal(McpScope.Project, s.Scope);
                    Assert.Equal("jd-canonical", s.SourceProvider);
                }
            }

            Assert.True(found);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task VsCodeProvider_ValidWorkspaceConfig_ReturnsServers()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var vscodeDir = Path.Combine(tempDir, ".vscode");
        Directory.CreateDirectory(vscodeDir);

        try
        {
            var configFile = Path.Combine(vscodeDir, "mcp.json");
            await File.WriteAllTextAsync(configFile, """
                {
                    "servers": {
                        "vscode-mcp": {
                            "command": "node",
                            "args": ["server.js"]
                        }
                    }
                }
                """);

            var provider = new VsCodeMcpDiscoveryProvider(workspaceRoot: tempDir);
            var results = await provider.DiscoverAsync();

            var found = false;
            foreach (var s in results)
            {
                if (string.Equals(s.Name, "vscode-mcp", System.StringComparison.Ordinal))
                {
                    found = true;
                    Assert.Equal("vscode", s.SourceProvider);
                    Assert.Equal(McpScope.Project, s.Scope);
                }
            }

            Assert.True(found);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Provider_MalformedConfig_DoesNotThrow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mcpJson = Path.Combine(tempDir, ".mcp.json");
            await File.WriteAllTextAsync(mcpJson, "{ this is not valid json }}}");

            var provider = new ClaudeCodeMcpDiscoveryProvider(workingDirectory: tempDir);

            // Should not throw
            var results = await provider.DiscoverAsync();
            Assert.NotNull(results);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ClaudeCodeProvider_ProviderId_IsCorrect()
    {
        var provider = new ClaudeCodeMcpDiscoveryProvider();
        Assert.Equal("claude-code", provider.ProviderId);
    }

    [Fact]
    public void ClaudeDesktopProvider_ProviderId_IsCorrect()
    {
        var provider = new ClaudeDesktopMcpDiscoveryProvider();
        Assert.Equal("claude-desktop", provider.ProviderId);
    }

    [Fact]
    public void VsCodeProvider_ProviderId_IsCorrect()
    {
        var provider = new VsCodeMcpDiscoveryProvider();
        Assert.Equal("vscode", provider.ProviderId);
    }

    [Fact]
    public void CodexProvider_ProviderId_IsCorrect()
    {
        var provider = new CodexMcpDiscoveryProvider();
        Assert.Equal("codex", provider.ProviderId);
    }

    [Fact]
    public void CopilotProvider_ProviderId_IsCorrect()
    {
        var provider = new CopilotMcpDiscoveryProvider();
        Assert.Equal("copilot", provider.ProviderId);
    }

    [Fact]
    public void JdCanonicalProvider_ProviderId_IsCorrect()
    {
        var provider = new JdCanonicalMcpDiscoveryProvider();
        Assert.Equal("jd-canonical", provider.ProviderId);
    }
}
