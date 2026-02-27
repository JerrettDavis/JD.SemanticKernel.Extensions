using System;
using JD.SemanticKernel.Extensions.Memory;
using JD.SemanticKernel.Extensions.Memory.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JD.SemanticKernel.Extensions.Memory.Sqlite.Tests;

public class SqliteServiceCollectionTests
{
    [Fact]
    public void AddSqliteMemoryBackend_RegistersBackend()
    {
        var services = new ServiceCollection();
        services.AddSqliteMemoryBackend("Data Source=:memory:");

        var provider = services.BuildServiceProvider();
        var backend = provider.GetRequiredService<IMemoryBackend>();

        Assert.IsType<SqliteMemoryBackend>(backend);
    }

    [Fact]
    public void AddSqliteMemoryBackend_NullServices_Throws()
    {
        IServiceCollection services = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddSqliteMemoryBackend("Data Source=:memory:"));
    }

    [Fact]
    public void AddSqliteMemoryBackend_EmptyConnectionString_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() => services.AddSqliteMemoryBackend(""));
    }

    [Fact]
    public void AddSqliteMemoryBackend_WhitespaceConnectionString_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() => services.AddSqliteMemoryBackend("   "));
    }

    [Fact]
    public void AddSqliteMemoryBackend_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddSqliteMemoryBackend("Data Source=:memory:");

        Assert.Same(services, result);
    }

    [Fact]
    public void AddSqliteMemoryBackendFromFile_RegistersBackend()
    {
        var services = new ServiceCollection();
        services.AddSqliteMemoryBackendFromFile("test.db");

        var provider = services.BuildServiceProvider();
        var backend = provider.GetRequiredService<IMemoryBackend>();

        Assert.IsType<SqliteMemoryBackend>(backend);
    }
}
