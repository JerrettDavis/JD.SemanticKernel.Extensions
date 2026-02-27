using JD.SemanticKernel.Extensions.Skills;
using Xunit;

namespace JD.SemanticKernel.Extensions.Skills.Tests;

public class SkillDefinitionTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var skill = new SkillDefinition();

        Assert.Equal(string.Empty, skill.Name);
        Assert.Equal(string.Empty, skill.Description);
        Assert.Equal(string.Empty, skill.Body);
        Assert.Empty(skill.AllowedTools);
        Assert.Empty(skill.Arguments);
        Assert.Empty(skill.Metadata);
        Assert.Null(skill.SourcePath);
    }

    [Fact]
    public void PropertyAssignment_Works()
    {
        var skill = new SkillDefinition
        {
            Name = "test-skill",
            Description = "A test skill",
            Body = "# Instructions\nDo something.",
            SourcePath = "/path/to/SKILL.md",
        };

        Assert.Equal("test-skill", skill.Name);
        Assert.Equal("A test skill", skill.Description);
        Assert.Equal("# Instructions\nDo something.", skill.Body);
        Assert.Equal("/path/to/SKILL.md", skill.SourcePath);
    }

    [Fact]
    public void AllowedTools_CanAddItems()
    {
        var skill = new SkillDefinition();
        skill.AllowedTools.Add("Read");
        skill.AllowedTools.Add("Write");

        Assert.Equal(2, skill.AllowedTools.Count);
        Assert.Contains("Read", skill.AllowedTools);
        Assert.Contains("Write", skill.AllowedTools);
    }

    [Fact]
    public void Arguments_CanAddEntries()
    {
        var skill = new SkillDefinition();
        skill.Arguments["ARGUMENTS"] = "all arguments";
        skill.Arguments["0"] = "first argument";

        Assert.Equal(2, skill.Arguments.Count);
        Assert.Equal("all arguments", skill.Arguments["ARGUMENTS"]);
    }

    [Fact]
    public void Metadata_CanAddEntries()
    {
        var skill = new SkillDefinition();
        skill.Metadata["context"] = "fork";
        skill.Metadata["custom"] = "value";

        Assert.Equal(2, skill.Metadata.Count);
    }
}
