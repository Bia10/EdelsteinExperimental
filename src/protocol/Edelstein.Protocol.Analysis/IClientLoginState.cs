namespace Edelstein.Protocol.Analysis;

/// <summary>A snapshot of the GMS v95 login-flow client state.</summary>
public interface IClientLoginState
{
    /// <summary>Current world index as read from <c>CWvsContext.WorldId</c>.</summary>
    int WorldId { get; }

    /// <summary>Current channel index as read from <c>CWvsContext.ChannelId</c>.</summary>
    int ChannelId { get; }

    /// <summary>Indicates whether the client is at the character-selection or in-game stage.</summary>
    bool IsLoggedIn { get; }
}
