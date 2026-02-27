using System;
using System.Collections.Generic;
using System.Linq;

namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// Maximal Marginal Relevance (MMR) reranker that balances relevance with diversity.
/// </summary>
public static class MmrReranker
{
    /// <summary>
    /// Reranks results using MMR to balance relevance and diversity.
    /// </summary>
    /// <param name="candidates">Candidate results with relevance scores.</param>
    /// <param name="queryEmbedding">The original query embedding.</param>
    /// <param name="lambda">Balance parameter (0.0 = all diversity, 1.0 = all relevance).</param>
    /// <param name="topK">Number of results to select.</param>
    /// <returns>Reranked results.</returns>
    public static IReadOnlyList<(MemoryRecord Record, double Score)> Rerank(
        IReadOnlyList<(MemoryRecord Record, double Score)> candidates,
        ReadOnlyMemory<float> queryEmbedding,
        double lambda = 0.7,
        int topK = 10)
    {
        if (candidates is null || candidates.Count == 0)
        {
            return Array.Empty<(MemoryRecord, double)>();
        }

        var selected = new List<(MemoryRecord Record, double Score)>();
        var remaining = new List<(MemoryRecord Record, double Score)>(candidates);

        while (selected.Count < topK && remaining.Count > 0)
        {
            var bestIdx = -1;
            var bestScore = double.MinValue;

            for (var i = 0; i < remaining.Count; i++)
            {
                var relevance = remaining[i].Score;

                // Calculate max similarity to already selected items
                var maxSimilarityToSelected = 0.0;
                foreach (var sel in selected)
                {
                    var sim = CosineSimilarity(remaining[i].Record.Embedding, sel.Record.Embedding);
                    if (sim > maxSimilarityToSelected)
                    {
                        maxSimilarityToSelected = sim;
                    }
                }

                var mmrScore = (lambda * relevance) - ((1.0 - lambda) * maxSimilarityToSelected);

                if (mmrScore > bestScore)
                {
                    bestScore = mmrScore;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0)
            {
                selected.Add(remaining[bestIdx]);
                remaining.RemoveAt(bestIdx);
            }
        }

        return selected;
    }

    private static double CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var spanA = a.Span;
        var spanB = b.Span;

        if (spanA.Length != spanB.Length || spanA.Length == 0)
        {
            return 0.0;
        }

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < spanA.Length; i++)
        {
            dot += spanA[i] * spanB[i];
            magA += spanA[i] * spanA[i];
            magB += spanB[i] * spanB[i];
        }

        var magnitude = Math.Sqrt(magA) * Math.Sqrt(magB);
        return magnitude > 0 ? dot / magnitude : 0.0;
    }
}
