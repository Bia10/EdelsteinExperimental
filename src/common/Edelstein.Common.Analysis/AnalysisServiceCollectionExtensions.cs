using Edelstein.Protocol.Analysis;
using Microsoft.Extensions.DependencyInjection;

namespace Edelstein.Common.Analysis;

/// <summary>Extension methods to register the client analysis services with the DI container.</summary>
public static class AnalysisServiceCollectionExtensions
{
    /// <summary>
    /// Registers client analysis services:
    /// <list type="bullet">
    ///   <item><see cref="IClientStructRegistry"/> — singleton, cross-platform struct/offset registry.</item>
    ///   <item><see cref="IClientAnalysisService"/> — singleton, Windows-only process analysis (attach before use).</item>
    ///   <item><see cref="IClientStringPoolService"/> — singleton, Windows-only PE-image StringPool decoder.</item>
    /// </list>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional delegate to configure <see cref="AnalysisOptions"/>.</param>
    public static IServiceCollection AddEdelsteinAnalysis(
        this IServiceCollection services,
        Action<AnalysisOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.AddOptions<AnalysisOptions>();

        services.AddSingleton<IClientStructRegistry, ClientStructRegistry>();

        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IClientAnalysisService, ClientAnalysisService>();
            services.AddSingleton<IClientStringPoolService, ClientStringPoolService>();
        }

        return services;
    }
}
