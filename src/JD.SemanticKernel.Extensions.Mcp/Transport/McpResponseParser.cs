using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace JD.SemanticKernel.Extensions.Mcp.Transport;

/// <summary>
/// Shared JSON-RPC response parsing utilities used by <see cref="StdioMcpClient"/> and <see cref="HttpMcpClient"/>.
/// </summary>
internal static class McpResponseParser
{
    internal static List<McpToolDefinition> ParseTools(JsonDocument response)
    {
        var results = new List<McpToolDefinition>();

        if (!response.RootElement.TryGetProperty("result", out var result))
            return results;

        if (!result.TryGetProperty("tools", out var toolsEl) ||
            toolsEl.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var toolEl in toolsEl.EnumerateArray())
        {
            if (!toolEl.TryGetProperty("name", out var nameEl) ||
                nameEl.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var name = nameEl.GetString()!;
            string? description = null;
            if (toolEl.TryGetProperty("description", out var descEl) &&
                descEl.ValueKind == JsonValueKind.String)
            {
                description = descEl.GetString();
            }

            var parameters = ParseToolParameters(toolEl);
            results.Add(new McpToolDefinition(name, description, parameters));
        }

        return results;
    }

    internal static List<McpToolParameter> ParseToolParameters(JsonElement toolEl)
    {
        var results = new List<McpToolParameter>();

        if (!toolEl.TryGetProperty("inputSchema", out var schemaEl) ||
            schemaEl.ValueKind != JsonValueKind.Object)
        {
            return results;
        }

        if (!schemaEl.TryGetProperty("properties", out var propsEl) ||
            propsEl.ValueKind != JsonValueKind.Object)
        {
            return results;
        }

        var requiredNames = new HashSet<string>(StringComparer.Ordinal);
        if (schemaEl.TryGetProperty("required", out var reqEl) &&
            reqEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var req in reqEl.EnumerateArray())
            {
                if (req.ValueKind == JsonValueKind.String)
                    requiredNames.Add(req.GetString()!);
            }
        }

        foreach (var prop in propsEl.EnumerateObject())
        {
            string? description = null;
            string? type = null;

            if (prop.Value.TryGetProperty("description", out var descEl) &&
                descEl.ValueKind == JsonValueKind.String)
            {
                description = descEl.GetString();
            }

            if (prop.Value.TryGetProperty("type", out var typeEl) &&
                typeEl.ValueKind == JsonValueKind.String)
            {
                type = typeEl.GetString();
            }

            results.Add(new McpToolParameter(
                prop.Name,
                description,
                type,
                requiredNames.Contains(prop.Name)));
        }

        return results;
    }

    internal static McpInvocationResult ParseInvocationResult(JsonDocument response)
    {
        var root = response.RootElement;

        if (root.TryGetProperty("error", out var errorEl))
        {
            var message = errorEl.TryGetProperty("message", out var msgEl)
                ? msgEl.GetString() ?? "Unknown error"
                : "Unknown error";
            return McpInvocationResult.Failure(message);
        }

        if (!root.TryGetProperty("result", out var result))
            return McpInvocationResult.Success(null);

        if (result.TryGetProperty("content", out var contentEl) &&
            contentEl.ValueKind == JsonValueKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var item in contentEl.EnumerateArray())
            {
                if (item.TryGetProperty("text", out var textEl) &&
                    textEl.ValueKind == JsonValueKind.String)
                {
                    sb.Append(textEl.GetString());
                }
            }

            return McpInvocationResult.Success(sb.Length > 0 ? sb.ToString() : null);
        }

        return McpInvocationResult.Success(result.GetRawText());
    }
}
