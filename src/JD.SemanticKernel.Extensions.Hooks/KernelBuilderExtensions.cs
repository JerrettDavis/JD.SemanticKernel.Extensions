using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Hooks;

/// <summary>
/// Extension methods for <see cref="IKernelBuilder"/> to register Claude Code hooks.
/// </summary>
public static class KernelBuilderExtensions
{
    /// <summary>
    /// Configures Claude Code lifecycle hooks on the kernel using a fluent builder.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="configure">Action to configure hooks via <see cref="HookBuilder"/>.</param>
    /// <returns>The kernel builder for chaining.</returns>
    public static IKernelBuilder UseHooks(
        this IKernelBuilder builder,
        Action<HookBuilder> configure)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
#else
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
#endif

        var hookBuilder = new HookBuilder();
        configure(hookBuilder);

        // Register function invocation filters
        foreach (var filter in hookBuilder.FunctionFilters)
            builder.Services.AddSingleton<IFunctionInvocationFilter>(filter);

        // Register prompt render filters
        foreach (var filter in hookBuilder.PromptFilters)
            builder.Services.AddSingleton<IPromptRenderFilter>(filter);

        // Register event bus with event handlers
        if (hookBuilder.EventHandlers.Count > 0)
        {
            var eventBus = new ExtensionEventBus();
            foreach (var handler in hookBuilder.EventHandlers)
                eventBus.Subscribe(handler);
            builder.Services.AddSingleton<IExtensionEventBus>(eventBus);
        }

        return builder;
    }

    /// <summary>
    /// Loads hooks from a Claude Code hooks.json file and registers them as SK filters.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="hooksFilePath">Path to the hooks.json file.</param>
    /// <returns>The kernel builder for chaining.</returns>
    public static IKernelBuilder UseHooksFile(
        this IKernelBuilder builder,
        string hooksFilePath)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(hooksFilePath);
#else
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (hooksFilePath is null) throw new ArgumentNullException(nameof(hooksFilePath));
#endif

        var hooks = HookParser.ParseFile(hooksFilePath);

        foreach (var hook in hooks)
        {
            switch (hook.Event)
            {
                case HookEvent.PreToolUse when hook.Type == HookType.Command:
                    builder.Services.AddSingleton<IFunctionInvocationFilter>(
                        new SkHookFilter(
                            preToolPattern: hook.ToolPattern ?? ".*",
                            preHandler: _ => CommandHookExecutor.ExecuteAsync(hook.Command!, hook.TimeoutMs)));
                    break;

                case HookEvent.PostToolUse when hook.Type == HookType.Command:
                    builder.Services.AddSingleton<IFunctionInvocationFilter>(
                        new SkHookFilter(
                            postToolPattern: hook.ToolPattern ?? ".*",
                            postHandler: _ => CommandHookExecutor.ExecuteAsync(hook.Command!, hook.TimeoutMs)));
                    break;
            }
        }

        return builder;
    }
}
