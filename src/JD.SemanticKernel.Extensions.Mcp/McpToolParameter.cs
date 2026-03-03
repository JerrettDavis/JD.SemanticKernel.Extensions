namespace JD.SemanticKernel.Extensions.Mcp;

/// <summary>
/// Describes a single parameter of an MCP tool.
/// </summary>
public sealed class McpToolParameter
{
    /// <summary>Gets the parameter name.</summary>
    public string Name { get; }

    /// <summary>Gets the parameter description.</summary>
    public string? Description { get; }

    /// <summary>Gets the JSON Schema type string (e.g., "string", "integer", "boolean").</summary>
    public string? Type { get; }

    /// <summary>Gets a value indicating whether this parameter is required.</summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="McpToolParameter"/>.
    /// </summary>
    public McpToolParameter(
        string name,
        string? description = null,
        string? type = null,
        bool isRequired = false)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
#else
        if (string.IsNullOrWhiteSpace(name)) throw new System.ArgumentException("Value cannot be null or whitespace.", nameof(name));
#endif

        Name = name;
        Description = description;
        Type = type;
        IsRequired = isRequired;
    }
}
