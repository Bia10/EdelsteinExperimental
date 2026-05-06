using Edelstein.Protocol.Analysis;
using Maple.Client.V95;
using Maple.Client.V95.Runtime;

namespace Edelstein.Common.Analysis;

/// <summary>Adapts a <see cref="ResolvedLoginState"/> to <see cref="IClientLoginState"/>.</summary>
internal sealed class ClientLoginState : IClientLoginState
{
    private readonly ResolvedLoginState _inner;

    internal ClientLoginState(ResolvedLoginState inner) => _inner = inner;

    public int WorldId => _inner.ContextWorldId ?? -1;
    public int ChannelId => _inner.ContextChannelId ?? -1;
    public bool IsLoggedIn => _inner.Step is LoginStep.SelectCharacter or LoginStep.Vac;
}
