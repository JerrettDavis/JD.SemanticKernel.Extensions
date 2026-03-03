using JD.SemanticKernel.Extensions.Hooks;

namespace JD.SemanticKernel.Extensions.Hooks.Tests;

public class HookParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsHookDefinitions()
    {
        var json = """
            {
                "hooks": [
                    {
                        "event": "PreToolUse",
                        "tool_name": "Bash",
                        "command": "echo validating"
                    },
                    {
                        "event": "PostToolUse",
                        "tool_name": "Write|Edit",
                        "command": "echo post-edit"
                    }
                ]
            }
            """;

        var hooks = HookParser.Parse(json);

        Assert.Equal(2, hooks.Count);
        Assert.Equal(HookEvent.PreToolUse, hooks[0].Event);
        Assert.Equal("Bash", hooks[0].ToolPattern);
        Assert.Equal(HookType.Command, hooks[0].Type);
        Assert.Equal(HookEvent.PostToolUse, hooks[1].Event);
        Assert.Equal("Write|Edit", hooks[1].ToolPattern);
    }

    [Fact]
    public void Parse_WithPromptHook_SetsPromptType()
    {
        var json = """
            {
                "hooks": [
                    {
                        "event": "PreToolUse",
                        "tool_name": "Bash",
                        "prompt": "Is this command safe? {{command}}"
                    }
                ]
            }
            """;

        var hooks = HookParser.Parse(json);

        Assert.Single(hooks);
        Assert.Equal(HookType.Prompt, hooks[0].Type);
        Assert.Contains("safe", hooks[0].Prompt);
    }

    [Fact]
    public void Parse_WithTimeoutMs_SetsTimeout()
    {
        var json = """
            {
                "hooks": [
                    {
                        "event": "PostToolUse",
                        "command": "echo done",
                        "timeout_ms": 5000
                    }
                ]
            }
            """;

        var hooks = HookParser.Parse(json);

        Assert.Single(hooks);
        Assert.Equal(5000, hooks[0].TimeoutMs);
    }

    [Fact]
    public void Parse_DefaultTimeout_Is30000()
    {
        var json = """
            {
                "hooks": [
                    {
                        "event": "SessionStart",
                        "command": "echo hello"
                    }
                ]
            }
            """;

        var hooks = HookParser.Parse(json);

        Assert.Equal(30000, hooks[0].TimeoutMs);
    }

    [Fact]
    public void Parse_EmptyHooks_ReturnsEmpty()
    {
        var json = """{"hooks": []}""";

        var hooks = HookParser.Parse(json);

        Assert.Empty(hooks);
    }

    [Fact]
    public void Parse_NullJson_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => HookParser.Parse(null!));
    }

    [Fact]
    public void Parse_AllEventTypes_MapsCorrectly()
    {
        var events = new[]
        {
            "PreToolUse", "PostToolUse", "UserPromptSubmit",
            "PreCompact", "SessionStart", "SessionEnd",
            "Stop", "SubagentStop", "Notification"
        };

        foreach (var evt in events)
        {
            var json = $$"""{"hooks": [{"event": "{{evt}}", "command": "echo"}]}""";
            var hooks = HookParser.Parse(json);
            Assert.Single(hooks);
        }
    }

    [Fact]
    public void Parse_ClaudeCodeNestedFormat_ReturnsHookDefinitions()
    {
        var json = """
            {
                "description": "Test plugin hooks",
                "hooks": {
                    "PreToolUse": [
                        {
                            "hooks": [
                                {
                                    "type": "command",
                                    "command": "python3 pretooluse.py",
                                    "timeout": 10
                                }
                            ]
                        }
                    ],
                    "PostToolUse": [
                        {
                            "hooks": [
                                {
                                    "type": "command",
                                    "command": "python3 posttooluse.py",
                                    "timeout": 5
                                }
                            ]
                        }
                    ]
                }
            }
            """;

        var hooks = HookParser.Parse(json);

        Assert.Equal(2, hooks.Count);
        Assert.Equal(HookEvent.PreToolUse, hooks[0].Event);
        Assert.Equal("python3 pretooluse.py", hooks[0].Command);
        Assert.Equal(10000, hooks[0].TimeoutMs); // 10 seconds → 10000ms
        Assert.Equal(HookEvent.PostToolUse, hooks[1].Event);
        Assert.Equal("python3 posttooluse.py", hooks[1].Command);
        Assert.Equal(5000, hooks[1].TimeoutMs);
    }

    [Fact]
    public void Parse_EmptyNestedFormat_ReturnsEmpty()
    {
        var json = """{"hooks": {}}""";

        var hooks = HookParser.Parse(json);

        Assert.Empty(hooks);
    }
}
