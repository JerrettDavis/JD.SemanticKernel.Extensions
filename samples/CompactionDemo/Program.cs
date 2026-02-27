using JD.SemanticKernel.Extensions.Compaction;
using Microsoft.SemanticKernel.ChatCompletion;

Console.WriteLine("=== JD.SemanticKernel.Extensions.Compaction Demo ===");
Console.WriteLine();

// Demonstrate token estimation
var history = new ChatHistory();
history.AddSystemMessage("You are a helpful assistant that provides detailed code reviews.");
history.AddUserMessage("Please review my C# code for the authentication module.");
history.AddAssistantMessage("I'd be happy to review your authentication module. Please share the code.");

for (var i = 0; i < 25; i++)
{
    history.AddUserMessage($"Here is method {i}: " + new string('x', 200));
    history.AddAssistantMessage($"Review of method {i}: The code looks good but consider " + new string('y', 200));
}

Console.WriteLine($"Chat history: {history.Count} messages");
Console.WriteLine($"Estimated tokens: {TokenEstimator.EstimateTokens(history):N0}");
Console.WriteLine();

// Demonstrate trigger evaluation
var options = new CompactionOptions
{
    TriggerMode = CompactionTriggerMode.ContextPercentage,
    Threshold = 0.70,
    MaxContextWindowTokens = 4_000, // Small window for demo
    PreserveLastMessages = 10,
    MinMessagesBeforeCompaction = 5,
};

var trigger = new ContextPercentageTrigger(options);
Console.WriteLine($"Trigger mode: {options.TriggerMode}");
Console.WriteLine($"Context window: {options.MaxContextWindowTokens:N0} tokens");
Console.WriteLine($"Threshold: {options.Threshold:P0}");
Console.WriteLine($"Should compact: {trigger.ShouldCompact(history)}");
Console.WriteLine();

// Demonstrate token threshold trigger
var tokenOptions = new CompactionOptions
{
    TriggerMode = CompactionTriggerMode.TokenThreshold,
    Threshold = 1000,
    MinMessagesBeforeCompaction = 5,
};

var tokenTrigger = new TokenThresholdTrigger(tokenOptions);
Console.WriteLine($"Token threshold trigger (threshold={tokenOptions.Threshold:N0}):");
Console.WriteLine($"Should compact: {tokenTrigger.ShouldCompact(history)}");
Console.WriteLine();

Console.WriteLine("Note: Full compaction with summarization requires a configured");
Console.WriteLine("IChatCompletionService in the Semantic Kernel. Register it with:");
Console.WriteLine("  builder.Services.AddCompaction(opt => { ... });");
Console.WriteLine();
Console.WriteLine("Demo complete!");
