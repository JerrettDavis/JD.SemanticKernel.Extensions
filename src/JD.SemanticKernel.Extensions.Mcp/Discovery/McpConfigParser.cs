using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JD.SemanticKernel.Extensions.Mcp.Discovery;

/// <summary>
/// Shared parsing utilities for the common <c>mcpServers</c> dictionary format
/// used by Claude Code, Claude Desktop, OpenAI Codex, and others.
/// </summary>
internal static class McpConfigParser
{
    /// <summary>
    /// Parses the <c>mcpServers</c> dictionary from a JSON document element
    /// into a list of <see cref="McpServerDefinition"/> instances.
    /// </summary>
    internal static List<McpServerDefinition> ParseMcpServers(
        JsonElement root,
        string providerId,
        string sourcePath,
        McpScope scope)
    {
        var results = new List<McpServerDefinition>();

        if (!root.TryGetProperty("mcpServers", out var serversElement) &&
            !root.TryGetProperty("mcp_servers", out serversElement))
        {
            return results;
        }

        if (serversElement.ValueKind != JsonValueKind.Object)
            return results;

        foreach (var serverProp in serversElement.EnumerateObject())
        {
            var name = serverProp.Name;
            var server = serverProp.Value;

            var definition = ParseServerEntry(name, server, providerId, sourcePath, scope);
            if (definition is not null)
                results.Add(definition);
        }

        return results;
    }

    internal static McpServerDefinition? ParseServerEntry(
        string name,
        JsonElement server,
        string providerId,
        string sourcePath,
        McpScope scope)
    {
        // Determine if server is disabled
        var isEnabled = true;
        if (server.TryGetProperty("disabled", out var disabledEl) && disabledEl.ValueKind == JsonValueKind.True)
            isEnabled = false;

        // Determine transport
        var transport = McpTransportType.Stdio;
        Uri? url = null;
        string? command = null;
        IReadOnlyList<string>? args = null;
        IReadOnlyDictionary<string, string>? env = null;

        if (server.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == JsonValueKind.String)
        {
            var urlStr = urlEl.GetString();
            if (!string.IsNullOrWhiteSpace(urlStr) && Uri.TryCreate(urlStr, UriKind.Absolute, out var parsedUrl))
            {
                url = parsedUrl;
                transport = McpTransportType.Http;
            }
            else if (server.TryGetProperty("command", out var cmdElFallback) && cmdElFallback.ValueKind == JsonValueKind.String)
            {
                // URL exists but is invalid; fall back to command/stdio if available.
                command = cmdElFallback.GetString();
                transport = McpTransportType.Stdio;

                if (server.TryGetProperty("args", out var argsElFallback) && argsElFallback.ValueKind == JsonValueKind.Array)
                {
                    var argList = new List<string>();
                    foreach (var arg in argsElFallback.EnumerateArray())
                    {
                        if (arg.ValueKind == JsonValueKind.String)
                            argList.Add(arg.GetString()!);
                    }
                    args = argList;
                }

                if (server.TryGetProperty("env", out var envElFallback) && envElFallback.ValueKind == JsonValueKind.Object)
                {
                    var envDict = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var envProp in envElFallback.EnumerateObject())
                    {
                        if (envProp.Value.ValueKind == JsonValueKind.String)
                            envDict[envProp.Name] = envProp.Value.GetString()!;
                    }
                    env = envDict;
                }
            }
            else
            {
                // URL is invalid and no command fallback; treat as unrecognized transport.
                return null;
            }
        }
        else if (server.TryGetProperty("command", out var cmdEl) && cmdEl.ValueKind == JsonValueKind.String)
        {
            command = cmdEl.GetString();
            transport = McpTransportType.Stdio;

            if (server.TryGetProperty("args", out var argsEl) && argsEl.ValueKind == JsonValueKind.Array)
            {
                var argList = new List<string>();
                foreach (var arg in argsEl.EnumerateArray())
                {
                    if (arg.ValueKind == JsonValueKind.String)
                        argList.Add(arg.GetString()!);
                }

                args = argList;
            }

            if (server.TryGetProperty("env", out var envEl) && envEl.ValueKind == JsonValueKind.Object)
            {
                var envDict = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var envProp in envEl.EnumerateObject())
                {
                    if (envProp.Value.ValueKind == JsonValueKind.String)
                        envDict[envProp.Name] = envProp.Value.GetString()!;
                }

                env = envDict;
            }
        }
        else
        {
            // No recognizable transport configuration
            return null;
        }

        var displayName = name;
        if (server.TryGetProperty("displayName", out var displayEl) && displayEl.ValueKind == JsonValueKind.String)
            displayName = displayEl.GetString() ?? name;

        return new McpServerDefinition(
            name: name,
            displayName: displayName,
            transport: transport,
            scope: scope,
            sourceProvider: providerId,
            sourcePath: sourcePath,
            url: url,
            command: command,
            args: args,
            env: env,
            isEnabled: isEnabled);
    }
}
