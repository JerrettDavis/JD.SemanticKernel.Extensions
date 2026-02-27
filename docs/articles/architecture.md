# Architecture

## Package Structure

```
JD.SemanticKernel.Extensions (meta-package)
├── JD.SemanticKernel.Extensions.Skills
│   ├── SkillParser — YAML frontmatter + markdown body parser
│   ├── SkillLoader — Directory scanner for SKILL.md files
│   └── SkillKernelFunction — Adapts skills → KernelFunction/KernelPlugin
├── JD.SemanticKernel.Extensions.Hooks
│   ├── HookParser — JSON parser for hooks.json
│   ├── SkHookFilter — IFunctionInvocationFilter with regex matching
│   ├── SkPromptHookFilter — IPromptRenderFilter
│   ├── HookBuilder — Fluent builder for custom hooks
│   └── ExtensionEventBus — Custom lifecycle event bus
└── JD.SemanticKernel.Extensions.Plugins
    ├── PluginManifest — plugin.json model
    ├── PluginLoader — Directory scanner + orchestrator
    └── PluginDependencyResolver — Topological sort
```

## Target Frameworks

All packages target `netstandard2.0` and `net8.0` for maximum compatibility.

## Claude Code Format Mapping

### SKILL.md → KernelFunction

The YAML frontmatter maps to function metadata. The markdown body becomes the prompt template. `$ARGUMENTS` maps to `{{$input}}`, positional args `$0`, `$1` map to `{{$arg0}}`, `{{$arg1}}`.

### hooks.json → SK Filters

`PreToolUse` and `PostToolUse` events become `IFunctionInvocationFilter` instances. Tool name patterns are matched via regex. Events without direct SK equivalents (SessionStart, PreCompact, etc.) are routed through `IExtensionEventBus`.

### plugin.json → KernelPlugin

The plugin manifest orchestrates loading skills and hooks from subdirectories. Dependencies are resolved via topological sort before loading.
