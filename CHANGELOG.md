# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial `JD.SemanticKernel.Extensions.Skills` package — load Claude Code SKILL.md files as SK KernelFunctions or PromptTemplates
- Initial `JD.SemanticKernel.Extensions.Hooks` package — map Claude Code lifecycle hooks to SK IFunctionInvocationFilter / IPromptRenderFilter
- Initial `JD.SemanticKernel.Extensions.Plugins` package — load `.claude-plugin/` directories into SK KernelPlugins
- Meta-package `JD.SemanticKernel.Extensions` with unified `.UseSkills()`, `.UsePlugins()`, `.UseHooks()` builder extensions
- Comprehensive unit tests for all packages
- DocFX documentation site
- GitHub Actions CI/CD pipeline
