using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JD.SemanticKernel.Extensions.Mcp.Transport;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Mcp.KernelIntegration;

/// <summary>
/// Factory for creating <see cref="KernelPlugin"/> instances from MCP server definitions.
/// </summary>
public static class McpKernelPluginFactory
{
    /// <summary>
    /// Creates a <see cref="KernelPlugin"/> that exposes all tools from the given MCP server.
    /// The server connection is established lazily on first tool invocation.
    /// </summary>
    /// <param name="server">The MCP server definition to create a plugin from.</param>
    /// <returns>A <see cref="KernelPlugin"/> containing one <see cref="KernelFunction"/> per MCP tool.</returns>
    public static async Task<KernelPlugin> FromMcpServerAsync(
        McpServerDefinition server,
        CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(server);
#else
        if (server is null) throw new ArgumentNullException(nameof(server));
#endif

        var client = CreateClient(server);

        try
        {
            await client.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var tools = await client.GetToolsAsync(cancellationToken).ConfigureAwait(false);

            var functions = new List<KernelFunction>(tools.Count);
            foreach (var tool in tools)
                functions.Add(CreateKernelFunction(tool, server, client));

            return KernelPluginFactory.CreateFromFunctions(
                NormalizeName(server.Name),
                server.DisplayName,
                functions);
        }
        catch
        {
            // Dispose client only if we failed to create a plugin;
            // on success, the functions hold a reference to it.
            if (client is IDisposable disposable)
                disposable.Dispose();
            throw;
        }
    }

    private static IMcpClient CreateClient(McpServerDefinition server) =>
        server.Transport switch
        {
            McpTransportType.Stdio => StdioMcpClient.FromDefinition(server),
            McpTransportType.Http => HttpMcpClient.FromDefinition(server),
            _ => throw new NotSupportedException($"Transport '{server.Transport}' is not supported.")
        };

    private static KernelFunction CreateKernelFunction(
        McpToolDefinition tool,
        McpServerDefinition server,
        IMcpClient client)
    {
        var parameters = tool.Parameters.Select(p => new KernelParameterMetadata(p.Name)
        {
            Description = p.Description,
            IsRequired = p.IsRequired,
            ParameterType = typeof(string),
        }).ToList();

        return KernelFunctionFactory.CreateFromMethod(
            method: async (KernelArguments args, CancellationToken ct) =>
            {
                var arguments = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in tool.Parameters)
                {
                    if (args.TryGetValue(p.Name, out var val))
                        arguments[p.Name] = val;
                }

                var result = await client.InvokeAsync(tool.Name, arguments, ct).ConfigureAwait(false);

                if (result.IsError)
                    throw new KernelException(result.ErrorMessage ?? "MCP tool invocation failed.");

                return result.Content;
            },
            functionName: NormalizeName(tool.Name),
            description: tool.Description ?? $"Invokes the '{tool.Name}' tool from MCP server '{server.DisplayName}'.",
            parameters: parameters,
            returnParameter: new KernelReturnParameterMetadata { Description = "Tool output" });
    }

    private static string NormalizeName(string name)
    {
        // MCP names may contain characters invalid for SK function names; normalize to alphanumeric + underscores.
        var chars = new char[name.Length];
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            chars[i] = char.IsLetterOrDigit(c) ? c : '_';
        }

        return new string(chars);
    }
}
