using JD.SemanticKernel.Extensions.Skills;

namespace JD.SemanticKernel.Extensions.Skills.Tests;

public class SkillLoaderTests
{
    [Fact]
    public void LoadFromDirectory_WithSkillFiles_ReturnsDefinitions()
    {
        var tempDir = CreateTempSkillDirectory();
        try
        {
            var skills = SkillLoader.LoadFromDirectory(tempDir);

            Assert.Single(skills);
            Assert.Equal("test-skill", skills[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadFromDirectory_Recursive_FindsNestedSkills()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var nestedDir = Path.Combine(tempDir, "sub", "deep");
        Directory.CreateDirectory(nestedDir);
        File.WriteAllText(Path.Combine(nestedDir, "SKILL.md"), """
            ---
            name: nested-skill
            description: A nested skill
            ---
            Nested content.
            """);

        try
        {
            var skills = SkillLoader.LoadFromDirectory(tempDir, recursive: true);
            Assert.Single(skills);
            Assert.Equal("nested-skill", skills[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadFromDirectory_NonRecursive_IgnoresNested()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var nestedDir = Path.Combine(tempDir, "sub");
        Directory.CreateDirectory(nestedDir);
        File.WriteAllText(Path.Combine(nestedDir, "SKILL.md"), """
            ---
            name: nested
            description: Nested
            ---
            Body.
            """);

        try
        {
            var skills = SkillLoader.LoadFromDirectory(tempDir, recursive: false);
            Assert.Empty(skills);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadFromDirectory_NonExistent_ThrowsDirectoryNotFound()
    {
        Assert.Throws<DirectoryNotFoundException>(
            () => SkillLoader.LoadFromDirectory("/nonexistent/path"));
    }

    [Fact]
    public void LoadFromFile_ValidFile_ReturnsDefinition()
    {
        var tempDir = CreateTempSkillDirectory();
        try
        {
            var skill = SkillLoader.LoadFromFile(
                Path.Combine(tempDir, "test-skill", "SKILL.md"));

            Assert.Equal("test-skill", skill.Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static string CreateTempSkillDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var skillDir = Path.Combine(tempDir, "test-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), """
            ---
            name: test-skill
            description: A test skill
            ---
            # Test Skill

            This is a test skill body.
            """);
        return tempDir;
    }
}
