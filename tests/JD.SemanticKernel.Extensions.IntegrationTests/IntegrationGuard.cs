using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.IntegrationTests;

/// <summary>
/// Guards and helpers for integration tests.
/// Set <c>EXTENSIONS_INTEGRATION_TESTS=true</c> to enable.
/// </summary>
internal static class IntegrationGuard
{
    private const string EnvVar = "EXTENSIONS_INTEGRATION_TESTS";

    public static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(EnvVar),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public static void EnsureEnabled() =>
        Xunit.Skip.IfNot(IsEnabled, $"Set {EnvVar}=true to run integration tests");
}

/// <summary>
/// Well-known Ollama configuration.
/// Model names and endpoint are overridable via environment variables
/// to support CI with smaller models.
/// </summary>
internal static class OllamaConfig
{
    private const string DefaultEndpoint = "http://localhost:11434/v1";
    private const string DefaultChatModel = "llama3.2:3b";
    private const string DefaultEmbeddingModel = "all-minilm:22m";

    public static string Endpoint =>
        Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") is { Length: > 0 } ep
            ? ep.TrimEnd('/') + "/v1"
            : DefaultEndpoint;

    public static string ChatModel =>
        Environment.GetEnvironmentVariable("OLLAMA_CHAT_MODEL") is { Length: > 0 } m
            ? m : DefaultChatModel;

    public static string EmbeddingModel =>
        Environment.GetEnvironmentVariable("OLLAMA_EMBEDDING_MODEL") is { Length: > 0 } m
            ? m : DefaultEmbeddingModel;

    public static bool IsAvailable()
    {
        try
        {
            var baseUrl = Endpoint.Replace("/v1", "", StringComparison.Ordinal);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = client.GetAsync($"{baseUrl}/api/tags").Result;
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a Kernel configured with Ollama's OpenAI-compatible chat endpoint.
    /// </summary>
    public static Kernel CreateChatKernel(string? model = null)
    {
        return Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: model ?? ChatModel,
                apiKey: "ollama",
                endpoint: new Uri(Endpoint))
            .Build();
    }

    /// <summary>
    /// Creates a Kernel configured with Ollama's OpenAI-compatible embedding endpoint.
    /// </summary>
#pragma warning disable SKEXP0010, CS0618
    public static Kernel CreateEmbeddingKernel(string? model = null)
    {
        return Kernel.CreateBuilder()
            .AddOpenAITextEmbeddingGeneration(
                modelId: model ?? EmbeddingModel,
                apiKey: "ollama",
                httpClient: CreateOllamaHttpClient())
            .Build();
    }

    /// <summary>
    /// Creates a Kernel configured with both chat and embedding from Ollama.
    /// </summary>
    public static Kernel CreateFullKernel(
        string? chatModel = null,
        string? embeddingModel = null)
    {
        return Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: chatModel ?? ChatModel,
                apiKey: "ollama",
                endpoint: new Uri(Endpoint))
            .AddOpenAITextEmbeddingGeneration(
                modelId: embeddingModel ?? EmbeddingModel,
                apiKey: "ollama",
                httpClient: CreateOllamaHttpClient())
            .Build();
    }
#pragma warning restore SKEXP0010, CS0618

    private static HttpClient CreateOllamaHttpClient() =>
        new() { BaseAddress = new Uri(Endpoint) };
}
