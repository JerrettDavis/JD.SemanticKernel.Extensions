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
/// </summary>
internal static class OllamaConfig
{
    public const string Endpoint = "http://localhost:11434/v1";
    public const string ChatModel = "llama3.2:3b";
    public const string EmbeddingModel = "all-minilm:22m";

    public static bool IsAvailable()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = client.GetAsync("http://localhost:11434/api/tags").Result;
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
    public static Kernel CreateChatKernel(string model = ChatModel)
    {
        return Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: model,
                apiKey: "ollama",
                endpoint: new Uri(Endpoint))
            .Build();
    }

    /// <summary>
    /// Creates a Kernel configured with Ollama's OpenAI-compatible embedding endpoint.
    /// </summary>
#pragma warning disable SKEXP0010, CS0618
    public static Kernel CreateEmbeddingKernel(string model = EmbeddingModel)
    {
        return Kernel.CreateBuilder()
            .AddOpenAITextEmbeddingGeneration(
                modelId: model,
                apiKey: "ollama",
                httpClient: CreateOllamaHttpClient())
            .Build();
    }

    /// <summary>
    /// Creates a Kernel configured with both chat and embedding from Ollama.
    /// </summary>
    public static Kernel CreateFullKernel(
        string chatModel = ChatModel,
        string embeddingModel = EmbeddingModel)
    {
        return Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: chatModel,
                apiKey: "ollama",
                endpoint: new Uri(Endpoint))
            .AddOpenAITextEmbeddingGeneration(
                modelId: embeddingModel,
                apiKey: "ollama",
                httpClient: CreateOllamaHttpClient())
            .Build();
    }
#pragma warning restore SKEXP0010, CS0618

    private static HttpClient CreateOllamaHttpClient() =>
        new() { BaseAddress = new Uri(Endpoint) };
}
