namespace JD.SemanticKernel.Extensions.Mcp;

/// <summary>
/// Represents the result of invoking an MCP tool.
/// </summary>
public sealed class McpInvocationResult
{
    /// <summary>Gets the raw string content returned by the tool.</summary>
    public string? Content { get; }

    /// <summary>Gets a value indicating whether the invocation resulted in an error.</summary>
    public bool IsError { get; }

    /// <summary>Gets the error message when <see cref="IsError"/> is <c>true</c>.</summary>
    public string? ErrorMessage { get; }

    private McpInvocationResult(string? content, bool isError, string? errorMessage)
    {
        Content = content;
        IsError = isError;
        ErrorMessage = errorMessage;
    }

    /// <summary>Creates a successful invocation result.</summary>
    public static McpInvocationResult Success(string? content) =>
        new(content, isError: false, errorMessage: null);

    /// <summary>Creates a failed invocation result.</summary>
    public static McpInvocationResult Failure(string errorMessage) =>
        new(content: null, isError: true, errorMessage: errorMessage);
}
