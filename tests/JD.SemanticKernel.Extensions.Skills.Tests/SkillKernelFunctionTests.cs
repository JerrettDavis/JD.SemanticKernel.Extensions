using JD.SemanticKernel.Extensions.Skills;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Skills.Tests;

public class SkillKernelFunctionTests
{
    [Fact]
    public void Create_ValidDefinition_ReturnsKernelFunction()
    {
        var definition = new SkillDefinition
        {
            Name = "test-func",
            Description = "A test function",
            Body = "Process this: $ARGUMENTS"
        };
        definition.Arguments["ARGUMENTS"] = "The input";

        var function = SkillKernelFunction.Create(definition);

        Assert.NotNull(function);
        Assert.Equal("test_func", function.Name);
        Assert.Equal("A test function", function.Description);
    }

    [Fact]
    public void Create_NullDefinition_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SkillKernelFunction.Create(null!));
    }

    [Fact]
    public void Create_WithPositionalArgs_AddsParameters()
    {
        var definition = new SkillDefinition
        {
            Name = "pos-args",
            Description = "Test",
            Body = "First: $0, Second: $1"
        };
        definition.Arguments["0"] = "First arg";
        definition.Arguments["1"] = "Second arg";

        var function = SkillKernelFunction.Create(definition);

        Assert.NotNull(function);
        Assert.NotNull(function.Metadata.Parameters);
    }

    [Fact]
    public void CreatePlugin_MultipleDefinitions_ReturnsPlugin()
    {
        var definitions = new[]
        {
            new SkillDefinition { Name = "skill-a", Description = "A", Body = "Body A" },
            new SkillDefinition { Name = "skill-b", Description = "B", Body = "Body B" }
        };

        var plugin = SkillKernelFunction.CreatePlugin("TestPlugin", definitions);

        Assert.NotNull(plugin);
        Assert.Equal("TestPlugin", plugin.Name);
        Assert.Equal(2, plugin.FunctionCount);
    }

    [Fact]
    public void Create_SanitizesFunctionName()
    {
        var definition = new SkillDefinition
        {
            Name = "my-awesome skill!",
            Description = "Test",
            Body = "Body"
        };

        var function = SkillKernelFunction.Create(definition);

        Assert.Equal("my_awesome_skill_", function.Name);
    }
}
