using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JD.SemanticKernel.Extensions.Mcp;
using JD.SemanticKernel.Extensions.Mcp.Registry;
using NSubstitute;

namespace JD.SemanticKernel.Extensions.Mcp.Tests;

public class McpRegistryTests
{
    private static McpServerDefinition MakeServer(
        string name,
        string provider,
        McpScope scope,
        bool isEnabled = true)
    {
        return new McpServerDefinition(
            name: name,
            displayName: name,
            transport: McpTransportType.Stdio,
            scope: scope,
            sourceProvider: provider,
            command: "echo",
            isEnabled: isEnabled);
    }

    [Fact]
    public async Task GetAllAsync_SingleProvider_ReturnsAllServers()
    {
        var provider = Substitute.For<IMcpDiscoveryProvider>();
        provider.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(
            new List<McpServerDefinition>
            {
                MakeServer("server-a", "p1", McpScope.User),
                MakeServer("server-b", "p1", McpScope.User),
            });

        var registry = new McpRegistry(new[] { provider });
        var results = await registry.GetAllAsync();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetAllAsync_MultipleProviders_MergesResults()
    {
        var p1 = Substitute.For<IMcpDiscoveryProvider>();
        p1.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(
            new List<McpServerDefinition> { MakeServer("server-a", "p1", McpScope.User) });

        var p2 = Substitute.For<IMcpDiscoveryProvider>();
        p2.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(
            new List<McpServerDefinition> { MakeServer("server-b", "p2", McpScope.User) });

        var registry = new McpRegistry(new[] { p1, p2 });
        var results = await registry.GetAllAsync();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, s => string.Equals(s.Name, "server-a", System.StringComparison.Ordinal));
        Assert.Contains(results, s => string.Equals(s.Name, "server-b", System.StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetAllAsync_ConflictingNames_HigherScopeWins()
    {
        var userServer = MakeServer("my-server", "user-provider", McpScope.User);
        var projectServer = MakeServer("my-server", "project-provider", McpScope.Project);

        var p1 = Substitute.For<IMcpDiscoveryProvider>();
        p1.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(
            new List<McpServerDefinition> { userServer });

        var p2 = Substitute.For<IMcpDiscoveryProvider>();
        p2.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(
            new List<McpServerDefinition> { projectServer });

        var registry = new McpRegistry(new[] { p1, p2 });
        var results = await registry.GetAllAsync();

        Assert.Single(results);
        Assert.Equal("project-provider", results[0].SourceProvider);
        Assert.Equal(McpScope.Project, results[0].Scope);
    }

    [Fact]
    public async Task GetAllAsync_SameScopeConflict_LastWriterWins()
    {
        // Both are User scope; second provider discovered wins (last-write)
        var first = MakeServer("shared", "provider-1", McpScope.User);
        var second = MakeServer("shared", "provider-2", McpScope.User);

        var p1 = Substitute.For<IMcpDiscoveryProvider>();
        p1.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(
            new List<McpServerDefinition> { first });

        var p2 = Substitute.For<IMcpDiscoveryProvider>();
        p2.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(
            new List<McpServerDefinition> { second });

        var registry = new McpRegistry(new[] { p1, p2 });
        var results = await registry.GetAllAsync();

        Assert.Single(results);
    }

    [Fact]
    public async Task GetAsync_ExistingServer_ReturnsDefinition()
    {
        var provider = Substitute.For<IMcpDiscoveryProvider>();
        provider.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(
            new List<McpServerDefinition> { MakeServer("notion", "p1", McpScope.User) });

        var registry = new McpRegistry(new[] { provider });
        var result = await registry.GetAsync("notion");

        Assert.NotNull(result);
        Assert.Equal("notion", result!.Name);
    }

    [Fact]
    public async Task GetAsync_NonExistentServer_ReturnsNull()
    {
        var provider = Substitute.For<IMcpDiscoveryProvider>();
        provider.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(
            new List<McpServerDefinition>());

        var registry = new McpRegistry(new[] { provider });
        var result = await registry.GetAsync("missing-server");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_CaseInsensitive_FindsServer()
    {
        var provider = Substitute.For<IMcpDiscoveryProvider>();
        provider.DiscoverAsync(Arg.Any<CancellationToken>()).Returns(
            new List<McpServerDefinition> { MakeServer("MyServer", "p1", McpScope.User) });

        var registry = new McpRegistry(new[] { provider });
        var result = await registry.GetAsync("myserver");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAllAsync_EmptyProviders_ReturnsEmpty()
    {
        var registry = new McpRegistry(new List<IMcpDiscoveryProvider>());
        var results = await registry.GetAllAsync();

        Assert.Empty(results);
    }
}
