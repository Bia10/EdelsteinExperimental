using Edelstein.Application.Server.Configs;
using Edelstein.Common.Services.Server;
using Edelstein.Common.Services.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Edelstein.Application.Server.Bootstraps;

public class CleanupRegistryBootstrap : IBootstrap
{
    private readonly ILogger<CleanupRegistryBootstrap> _logger;
    private readonly IDbContextFactory<ServerDbContext> _dbFactory;
    private readonly ProgramConfig _config;

    public CleanupRegistryBootstrap(
        ILogger<CleanupRegistryBootstrap> logger,
        IDbContextFactory<ServerDbContext> dbFactory,
        ProgramConfig config)
    {
        _logger = logger;
        _dbFactory = dbFactory;
        _config = config;
    }

    public int Priority => BootstrapPriority.Init - 1;

    public async Task Start()
    {
        if (!_config.CleanupRegistryOnInit) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var stageIDs = _config.LoginStages.Select(static s => s.ID)
            .Concat(_config.GameStages.Select(static s => s.ID))
            .Concat(_config.ShopStages.Select(static s => s.ID))
            .Concat(_config.TradeStages.Select(static s => s.ID))
            .ToHashSet(StringComparer.Ordinal);
        var gameStages = _config.GameStages
            .Select(static s => (s.WorldID, s.ChannelID))
            .ToHashSet();
        var shopStages = _config.ShopStages
            .Select(static s => s.WorldID)
            .ToHashSet();
        var tradeStages = _config.TradeStages
            .Select(static s => s.WorldID)
            .ToHashSet();
        var entries = await db.Servers
            .ToListAsync();
        var removable = entries
            .Where(s => s.DateExpire < now || stageIDs.Contains(s.ID) || s switch
            {
                ServerGameEntity game => gameStages.Contains((game.WorldID, game.ChannelID)),
                ServerShopEntity shop => shopStages.Contains(shop.WorldID),
                ServerTradeEntity trade => tradeStages.Contains(trade.WorldID),
                _ => false
            })
            .DistinctBy(static s => s.ID)
            .ToList();

        if (removable.Count != 0)
        {
            db.Servers.RemoveRange(removable);
            await db.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} registry entries on init", removable.Count);
        }
    }

    public Task Stop() => Task.CompletedTask;
}
