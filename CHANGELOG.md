# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `JD.SemanticKernel.Extensions.Compaction` package — transparent context window compaction middleware
  - `CompactionFilter` using `IAutoFunctionInvocationFilter` for zero-code-change integration
  - `TokenThresholdTrigger` and `ContextPercentageTrigger` for configurable compaction triggers
  - `HierarchicalSummarizationStrategy` for progressive message summarization
  - `TokenEstimator` for character-based token count estimation
  - `AddCompaction()` DI extension method
- `JD.SemanticKernel.Extensions.Memory` package — semantic memory with advanced search
  - `SemanticMemory` orchestrator with embedding generation and search pipeline
  - `InMemoryBackend` for development and testing
  - `MmrReranker` for Maximal Marginal Relevance diversity reranking
  - `TemporalDecayScorer` for time-weighted relevance scoring
  - `QueryExpander` for broadening search recall
  - `AddSemanticMemory()` DI extension method
- `JD.SemanticKernel.Extensions.Memory.Sqlite` package — SQLite-backed persistent memory
  - `SqliteMemoryBackend` with blob-stored embeddings and managed cosine similarity
  - `AddSqliteMemoryBackend()` DI extension method
- Updated meta-package to include Compaction and Memory packages
- CompactionDemo and MemoryDemo sample applications
- 35 new unit tests (86 total across solution)
- Design document for Compaction and Memory architecture
- GitHub Actions workflows: CodeQL, dependency-review, docs, labeler, PR validation, stale
- Issue templates, PR template, CODEOWNERS, and labeler configuration

### Previous

- Initial `JD.SemanticKernel.Extensions.Skills` package — load Claude Code SKILL.md files as SK KernelFunctions or PromptTemplates
- Initial `JD.SemanticKernel.Extensions.Hooks` package — map Claude Code lifecycle hooks to SK IFunctionInvocationFilter / IPromptRenderFilter
- Initial `JD.SemanticKernel.Extensions.Plugins` package — load `.claude-plugin/` directories into SK KernelPlugins
- Meta-package `JD.SemanticKernel.Extensions` with unified `.UseSkills()`, `.UsePlugins()`, `.UseHooks()` builder extensions
- Comprehensive unit tests for all packages
- DocFX documentation site
- GitHub Actions CI/CD pipeline
