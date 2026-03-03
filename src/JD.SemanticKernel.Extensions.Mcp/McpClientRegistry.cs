using System;
using System.Collections.Generic;

namespace JD.SemanticKernel.Extensions.Mcp;

/// <summary>
/// Tracks <see cref="IMcpClient"/> instances created during MCP plugin registration so that
/// they can be disposed when the application's <see cref="IServiceProvider"/> is disposed.
/// Register this as a singleton via <see cref="KernelBuilderMcpExtensions.AddMcpDiscovery"/>.
/// </summary>
public sealed class McpClientRegistry : IDisposable
{
    private readonly List<IDisposable> _clients = new List<IDisposable>();
    private readonly object _lock = new object();
    private bool _disposed;

    /// <summary>
    /// Registers a disposable MCP client for cleanup when this collection is disposed.
    /// </summary>
    /// <param name="client">The disposable client to track.</param>
    public void Add(IDisposable client)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(client);
#else
        if (client is null) throw new ArgumentNullException(nameof(client));
#endif

        lock (_lock)
        {
            if (_disposed)
                client.Dispose();
            else
                _clients.Add(client);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
            foreach (var client in _clients)
            {
#pragma warning disable CA1031
                try { client.Dispose(); }
                catch { /* suppress errors during shutdown */ }
#pragma warning restore CA1031
            }

            _clients.Clear();
        }
    }
}
