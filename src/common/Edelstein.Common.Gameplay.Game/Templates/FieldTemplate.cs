using System.Collections.Frozen;
using Duey.Abstractions;
using Edelstein.Common.Gameplay.Game.Security;
using Edelstein.Common.Gameplay.Game.Spatial;
using Edelstein.Common.Utilities.Spatial;
using Edelstein.Protocol.Gameplay.Game.Spatial;
using Edelstein.Protocol.Gameplay.Game.Templates;
using Edelstein.Protocol.Utilities.Spatial;

namespace Edelstein.Common.Gameplay.Game.Templates;

public record FieldTemplate : IFieldTemplate
{
    public FieldTemplate(
        int id,
        IDataNode foothold,
        IDataNode portal,
        IDataNode ladderRope,
        IDataNode life,
        IDataNode info
    )
    {
        ID = id;

        Limit = (FieldLimitType)(info.ResolveInt("fieldLimit") ?? 0);

        FieldReturn = info.ResolveInt("returnMap");
        ForcedReturn = info.ResolveInt("forcedReturn");
        if (FieldReturn == 999999999)
            FieldReturn = null;
        if (ForcedReturn == 999999999)
            ForcedReturn = null;

        ScriptFirstUserEnter = info.ResolveString("onFirstUserEnter");
        ScriptUserEnter = info.ResolveString("onUserEnter");
        if (string.IsNullOrWhiteSpace(ScriptFirstUserEnter))
            ScriptFirstUserEnter = null;
        if (string.IsNullOrWhiteSpace(ScriptUserEnter))
            ScriptUserEnter = null;

        var footholds = foothold
            .Children.SelectMany(c => c.Children)
            .SelectMany(c => c.Children)
            .Select(p => new FieldFoothold(Convert.ToInt32(p.Name), p.Cache()))
            .ToFrozenSet();
        var portals = portal
            .Children.Select(p => new FieldPortal(Convert.ToInt32(p.Name), p.Cache()))
            .ToFrozenSet();

        var leftTop = new Point2D(footholds.Min(f => f.MinX), footholds.Min(f => f.MinY));
        var rightBottom = new Point2D(footholds.Max(f => f.MaxX), footholds.Max(f => f.MaxY));

        leftTop = new Point2D(
            info.ResolveInt("VRLeft") ?? leftTop.X,
            info.ResolveInt("VRTop") ?? leftTop.Y
        );
        rightBottom = new Point2D(
            info.ResolveInt("VRRight") ?? rightBottom.X,
            info.ResolveInt("VRBottom") ?? rightBottom.Y
        );

        Bounds = new Rectangle2D(leftTop, rightBottom);

        Footholds = new FieldSpace<IFieldFoothold>(Bounds);
        Footholds.Insert(footholds);

        StartPoints = new FieldSpace<IFieldPortal>(Bounds);
        StartPoints.Insert(portals.Where(p => p.Type == FieldPortalType.StartPoint));

        Portals = new FieldSpace<IFieldPortal>(Bounds);
        Portals.Insert(portals);

        Life = life
            .Children.Select(p => new FieldTemplateLife(p.Cache()))
            .ToFrozenSet<IFieldTemplateLife>();

        MobRate = info.ResolveDouble("mobRate") ?? 1.0;

        var mobCapacity = Bounds.Height * Bounds.Width * MobRate * 0.0000078125;

        mobCapacity = Math.Min(mobCapacity, 40);
        mobCapacity = Math.Max(mobCapacity, 1);

        MobCapacityMin = (int)mobCapacity;
        MobCapacityMax = (int)mobCapacity * 2;

        // ── Field CRC (§7–§9 in V95_CRC_Complete_Reference.md) ──────────────
        Town = (info.ResolveInt("town") ?? 0) != 0 ? 1 : 0;
        Swim = (info.ResolveInt("swim") ?? 0) != 0 ? 1 : 0;
        Fly = (info.ResolveInt("fly") ?? 0) != 0 ? 1 : 0;
        PersonalShopAvailable = (Limit & FieldLimitType.MinigameLimit) == 0 ? 1 : 0;
        Phase = info.ResolveInt("phase") ?? 0;

        var fhEntries = footholds
            .OrderBy(f => f.ID)
            .Select(f => new FootholdEntry(
                f.ID,
                f.Line.P1.X,
                f.Line.P1.Y,
                f.Line.P2.X,
                f.Line.P2.Y,
                f.Drag,
                f.Force,
                f.ForbidFallDown,
                f.CantThrough,
                f.PrevID,
                f.NextID
            ))
            .ToArray();

        var ptEntries = portals
            .OrderBy(p => p.ID)
            .Select(p => new PortalEntry(
                p.Name,
                (int)p.Type,
                p.Position.X,
                p.Position.Y,
                p.HRange,
                p.VRange,
                p.ToMap,
                p.ToName ?? string.Empty,
                p.DelayTime,
                p.OnlyOnce,
                p.VImpact,
                p.HImpact
            ))
            .ToArray();

        var footholdCrc = V95Crc.CalcFootholdCrc(fhEntries);
        var portalCrc = V95Crc.CalcPortalCrc(id, ptEntries);
        CrcValue = V95Crc.CalcFieldCrc(
            footholdCrc,
            portalCrc,
            Town,
            Swim,
            Fly,
            PersonalShopAvailable,
            Phase
        );
    }

    public int ID { get; }

    public FieldLimitType Limit { get; }

    public IRectangle2D Bounds { get; }

    public IFieldSpace<IFieldFoothold> Footholds { get; }
    public IFieldSpace<IFieldPortal> StartPoints { get; }
    public IFieldSpace<IFieldPortal> Portals { get; }

    public int? FieldReturn { get; }
    public int? ForcedReturn { get; }

    public string? ScriptFirstUserEnter { get; }
    public string? ScriptUserEnter { get; }

    public ICollection<IFieldTemplateLife> Life { get; }

    public double MobRate { get; }
    public int MobCapacityMin { get; }
    public int MobCapacityMax { get; }

    /// <summary>1 if this map is a town, else 0 (WZ info/town).</summary>
    public int Town { get; }

    /// <summary>1 if this map is a swimming map, else 0 (WZ info/swim).</summary>
    public int Swim { get; }

    /// <summary>1 if this map allows free flying, else 0 (WZ info/fly).</summary>
    public int Fly { get; }

    /// <summary>1 if personal shops are permitted (MinigameLimit not set).</summary>
    public int PersonalShopAvailable { get; }

    /// <summary>Phase number for instanced maps (WZ info/phase).</summary>
    public int Phase { get; }

    /// <summary>
    /// Field composite CRC (m_dwCrc) — embedded in every movement packet.
    /// Computed from footholds, portals, and map attributes using CRC-32/BZIP2.
    /// </summary>
    public uint CrcValue { get; }
}
