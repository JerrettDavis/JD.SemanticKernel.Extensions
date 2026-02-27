# Plugin Demo

Demonstrates loading a Claude Code plugin directory into Semantic Kernel.

See [`samples/PluginDemo/Program.cs`](https://github.com/JerrettDavis/JD.SemanticKernel.Extensions/tree/main/samples/PluginDemo) for the full source.

## What it does

1. Creates a sample plugin.json manifest with skills and hooks subdirectories
2. Loads it using `AddClaudeCodePlugin()` extension method
3. Displays loaded plugins, skills, and hooks
