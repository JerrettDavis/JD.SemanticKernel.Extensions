using System;
using Xunit;

namespace JD.SemanticKernel.Extensions.Memory.Tests;

public class MemoryRecordTests
{
    private static readonly float[] TestEmbeddingValues = [1.0f, 2.0f, 3.0f];

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var record = new MemoryRecord();

        Assert.Equal(string.Empty, record.Id);
        Assert.Equal(string.Empty, record.Text);
        Assert.Empty(record.Metadata);
        Assert.True(record.Embedding.IsEmpty);
    }

    [Fact]
    public void PropertyAssignment_Works()
    {
        var now = DateTimeOffset.UtcNow;
        var embedding = new ReadOnlyMemory<float>(TestEmbeddingValues);

        var record = new MemoryRecord
        {
            Id = "test-id",
            Text = "test text",
            Embedding = embedding,
            CreatedAt = now,
            LastAccessedAt = now,
        };

        Assert.Equal("test-id", record.Id);
        Assert.Equal("test text", record.Text);
        Assert.Equal(3, record.Embedding.Length);
        Assert.Equal(now, record.CreatedAt);
        Assert.Equal(now, record.LastAccessedAt);
    }

    [Fact]
    public void Metadata_CanAddEntries()
    {
        var record = new MemoryRecord();
        record.Metadata["key1"] = "value1";
        record.Metadata["key2"] = "value2";

        Assert.Equal(2, record.Metadata.Count);
        Assert.Equal("value1", record.Metadata["key1"]);
    }
}
