using JD.SemanticKernel.Extensions.Skills;

namespace JD.SemanticKernel.Extensions.Skills.Tests;

public class SkillParserTests
{
    [Fact]
    public void Parse_WithFrontmatter_ExtractsNameAndDescription()
    {
        var content = """
            ---
            name: code-reviewer
            description: Reviews code for quality issues
            ---
            # Code Reviewer

            Review the code.
            """;

        var skill = SkillParser.Parse(content);

        Assert.Equal("code-reviewer", skill.Name);
        Assert.Equal("Reviews code for quality issues", skill.Description);
        Assert.Contains("Review the code.", skill.Body);
    }

    [Fact]
    public void Parse_WithAllowedTools_ParsesList()
    {
        var content = """
            ---
            name: test-skill
            description: Test
            allowed-tools:
              - Read
              - Grep
              - Glob
            ---
            Body content.
            """;

        var skill = SkillParser.Parse(content);

        Assert.Equal(3, skill.AllowedTools.Count);
        Assert.Contains("Read", skill.AllowedTools);
        Assert.Contains("Grep", skill.AllowedTools);
        Assert.Contains("Glob", skill.AllowedTools);
    }

    [Fact]
    public void Parse_WithArguments_ExtractsArgumentReferences()
    {
        var content = """
            ---
            name: arg-skill
            description: Uses arguments
            ---
            Process this: $ARGUMENTS
            First: $0
            Second: $1
            """;

        var skill = SkillParser.Parse(content);

        Assert.True(skill.Arguments.ContainsKey("ARGUMENTS"));
        Assert.True(skill.Arguments.ContainsKey("0"));
        Assert.True(skill.Arguments.ContainsKey("1"));
    }

    [Fact]
    public void Parse_WithoutFrontmatter_UsesHeadingAsName()
    {
        var content = """
            # My Awesome Skill

            This skill does something.
            """;

        var skill = SkillParser.Parse(content);

        Assert.Equal("my-awesome-skill", skill.Name);
        Assert.Contains("This skill does something.", skill.Body);
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsUnnamedSkill()
    {
        var skill = SkillParser.Parse("");

        Assert.Equal("unnamed-skill", skill.Name);
        Assert.Empty(skill.Body);
    }

    [Fact]
    public void Parse_NullContent_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SkillParser.Parse(null!));
    }

    [Fact]
    public void Parse_WithMetadata_CapturesExtraProperties()
    {
        var content = """
            ---
            name: meta-skill
            description: Has metadata
            context: fork
            custom-prop: value
            ---
            Body.
            """;

        var skill = SkillParser.Parse(content);

        Assert.True(skill.Metadata.ContainsKey("context"));
        Assert.True(skill.Metadata.ContainsKey("custom-prop"));
    }

    [Fact]
    public void Parse_SetsSourcePath()
    {
        var skill = SkillParser.Parse("Content", "/path/to/SKILL.md");

        Assert.Equal("/path/to/SKILL.md", skill.SourcePath);
    }

    [Fact]
    public void Parse_CommaSeparatedAllowedTools_ParsesList()
    {
        var content = """
            ---
            name: comma-tools
            description: Test
            allowed-tools: Read,Grep,Glob
            ---
            Body.
            """;

        var skill = SkillParser.Parse(content);

        Assert.Equal(3, skill.AllowedTools.Count);
    }
}
