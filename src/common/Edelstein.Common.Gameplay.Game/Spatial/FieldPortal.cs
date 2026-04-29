using Duey.Abstractions;
using Edelstein.Common.Utilities.Spatial;
using Edelstein.Protocol.Gameplay.Game.Spatial;
using Edelstein.Protocol.Gameplay.Game.Templates;
using Edelstein.Protocol.Utilities.Spatial;

namespace Edelstein.Common.Gameplay.Game.Spatial;

public record FieldPortal : IFieldPortal
{
    public FieldPortal(int id, IDataNode node)
    {
        ID = id;

        Name = node.ResolveString("pn")!;
        Type = (FieldPortalType)(node.ResolveInt("pt") ?? 0);
        Script = node.ResolveString("script");
        ToMap = node.ResolveInt("tm") ?? int.MinValue;
        ToName = node.ResolveString("tn");
        Position = new Point2D(
            node.ResolveInt("x") ?? int.MinValue,
            node.ResolveInt("y") ?? int.MinValue
        );

        HRange = node.ResolveInt("hRange") ?? 0;
        VRange = node.ResolveInt("vRange") ?? 0;
        DelayTime = node.ResolveInt("delay") ?? 0;
        OnlyOnce = (node.ResolveInt("onlyOnce") ?? 0) != 0;
        VImpact = node.ResolveInt("vImpact") ?? 0;
        HImpact = node.ResolveInt("hImpact") ?? 0;
    }

    public int ID { get; }

    public int MinX => Position.MinX;
    public int MinY => Position.MinY;

    public int MaxX => Position.MaxX;
    public int MaxY => Position.MaxY;

    public FieldPortalType Type { get; }

    public string Name { get; }
    public string? Script { get; }

    public int ToMap { get; }
    public string? ToName { get; }

    public IPoint2D Position { get; }

    public int HRange { get; }
    public int VRange { get; }
    public int DelayTime { get; }
    public bool OnlyOnce { get; }
    public int VImpact { get; }
    public int HImpact { get; }
}
