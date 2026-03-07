namespace Edelstein.Common.Services.Social;

/// <summary>
/// Server-side configuration knobs for the guild system.
/// Bind this from the <c>"Guild"</c> section in <c>appsettings.json</c>.
/// </summary>
public class GuildOptions
{
    /// <summary>
    /// Meso fee deducted from the party leader when a guild is created.
    /// V95 retail value: 1 500 000.
    /// </summary>
    public int CreationFee { get; set; } = 1_500_000;

    /// <summary>
    /// Minimum number of characters in a guild name (inclusive).
    /// </summary>
    public int MinNameLength { get; set; } = 2;

    /// <summary>
    /// Maximum number of characters in a guild name (inclusive).
    /// </summary>
    public int MaxNameLength { get; set; } = 12;

    /// <summary>
    /// Number of party members (including the leader) required to found a guild.
    /// Set to 1 to allow solo creation.
    /// </summary>
    public int RequiredPartySize { get; set; } = 6;

    /// <summary>
    /// Starting member capacity for newly created guilds.
    /// Can later be increased in-game via the guild-capacity upgrade.
    /// </summary>
    public int DefaultMaxMemberNum { get; set; } = 10;

    /// <summary>
    /// How long (in minutes) a pending guild invitation remains valid before it expires.
    /// </summary>
    public int InviteExpiryMinutes { get; set; } = 3;

    /// <summary>
    /// Hard cap on guild member capacity; expansions cannot exceed this value.
    /// V95 retail value: 200.
    /// </summary>
    public int MaxMemberNum { get; set; } = 200;

    /// <summary>
    /// Number of member slots added per capacity expansion (paid via <see cref="ExpandFee"/>).
    /// </summary>
    public int ExpandStep { get; set; } = 5;

    /// <summary>
    /// Meso fee deducted from the guild master for each capacity expansion.
    /// V95 retail value: 500 000.
    /// </summary>
    public int ExpandFee { get; set; } = 500_000;

    /// <summary>
    /// Meso fee deducted from the guild master to create a guild emblem.
    /// V95 retail value: 5 000 000.
    /// </summary>
    public int CreateEmblemFee { get; set; } = 5_000_000;

    /// <summary>
    /// Meso fee deducted from the guild master to delete the guild emblem.
    /// V95 retail value: 1 000 000.
    /// </summary>
    public int DeleteEmblemFee { get; set; } = 1_000_000;
}
