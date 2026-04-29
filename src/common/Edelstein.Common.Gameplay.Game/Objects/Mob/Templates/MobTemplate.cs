using System.Collections.Immutable;
using Duey.Abstractions;
using Edelstein.Common.Gameplay.Game.Security;
using Edelstein.Protocol.Gameplay.Game.Objects;
using Edelstein.Protocol.Gameplay.Game.Objects.Mob.Templates;
using Edelstein.Protocol.Gameplay.Models.Characters.Skills.Templates;

namespace Edelstein.Common.Gameplay.Game.Objects.Mob.Templates;

public class MobTemplate : IMobTemplate
{
    public MobTemplate(int id, IDataNode node, IDataNode info)
    {
        ID = id;

        if (node.ResolvePath("fly") != null)
            MoveAbility = MoveAbilityType.Fly;
        else if (node.ResolvePath("jump") != null)
            MoveAbility = MoveAbilityType.Jump;
        else if (node.ResolvePath("move") != null)
            MoveAbility = MoveAbilityType.Walk;
        else
            MoveAbility = MoveAbilityType.Stop;

        Level = info.ResolveShort("level") ?? 0;

        IsBoss = (info.ResolveInt("boss") ?? 0) > 0;

        MaxHP = info.ResolveInt("maxHP") ?? 1;
        MaxMP = info.ResolveInt("maxMP") ?? 0;

        PAD = info.ResolveInt("PADamage") ?? 0;
        PDD = info.ResolveInt("PDDamage") ?? 0;
        PDR = info.ResolveInt("PDRate") ?? 0;
        MAD = info.ResolveInt("MADamage") ?? 0;
        MDD = info.ResolveInt("PDDamage") ?? 0;
        MDR = info.ResolveInt("MDRate") ?? 0;
        ACC = info.ResolveInt("acc") ?? 0;
        EVA = info.ResolveInt("eva") ?? 0;

        EXP = info.ResolveInt("exp") ?? 0;

        ElementAttributes = new Dictionary<Element, ElementAttribute>
        {
            { Element.Physical, ElementAttribute.None },
            { Element.Ice, ElementAttribute.None },
            { Element.Fire, ElementAttribute.None },
            { Element.Light, ElementAttribute.None },
            { Element.Poison, ElementAttribute.None },
            { Element.Holy, ElementAttribute.None },
            { Element.Dark, ElementAttribute.None },
            { Element.Undead, ElementAttribute.None },
        };

        var elemCount = 0;
        var elemAttrs = info.ResolveString("elemAttr") ?? string.Empty;

        foreach (var group in elemAttrs.GroupBy(_ => elemCount++ / 2).ToImmutableArray())
        {
            var groupList = group.ToImmutableArray();
            var elem = groupList[0] switch
            {
                'P' => Element.Physical,
                'I' => Element.Ice,
                'F' => Element.Fire,
                'L' => Element.Light,
                'S' => Element.Poison,
                'H' => Element.Holy,
                'D' => Element.Dark,
                'U' => Element.Undead,
                _ => Element.Physical,
            };
            var elemAttr = (ElementAttribute)Convert.ToInt32(groupList[1].ToString());

            ElementAttributes[elem] = elemAttr;
        }

        Speed = info.ResolveInt("speed") ?? 0;
        FlySpeed = info.ResolveInt("flySpeed") ?? 0;
        ChaseSpeed = info.ResolveInt("chaseSpeed") ?? 0;
        BodyAttack = (info.ResolveInt("bodyAttack") ?? 0) != 0;
        OnlyNormalAttack = (info.ResolveInt("onlyNormalAttack") ?? 0) != 0;
        NotAttack = (info.ResolveInt("notAttack") ?? 0) != 0;
        SelfDestructionMob = (info.ResolveInt("selfDestruction") ?? 0) != 0;
        PickUpDrop = (info.ResolveInt("pickUpItem") ?? 0) != 0;
        EscortType = info.ResolveInt("escort") ?? 0;
        HPRecovery = info.ResolveInt("hpRecovery") ?? 0;
        MPRecovery = info.ResolveInt("mpRecovery") ?? 0;
        FirstAttack = (info.ResolveInt("firstAttack") ?? 0) != 0;
        Invincible = (info.ResolveInt("invincible") ?? 0) != 0;
        FixedDamage = info.ResolveInt("fixedDamage") ?? 0;
        PushedDamage = info.ResolveInt("pushed") ?? 0;
        MobFs = info.ResolveDouble("fs") ?? 0.0;

        // Build attack info array (attack1, attack2, ...)
        var attacks = new List<MobAttackEntry>();
        for (var i = 1; ; i++)
        {
            var attackNode = node.ResolvePath($"attack{i}");
            if (attackNode == null)
                break;
            attacks.Add(
                new MobAttackEntry
                {
                    NType = attackNode.ResolveInt("type") ?? 0,
                    BInactive = (attackNode.ResolveInt("inactive") ?? 0) != 0,
                    NConMP = attackNode.ResolveInt("conMP") ?? 0,
                    BMagicAttack = (attackNode.ResolveInt("magic") ?? 0) != 0,
                    BJumpAttack = (attackNode.ResolveInt("jumpAttack") ?? 0) != 0,
                    NBulletSpeed = attackNode.ResolveInt("bulletSpeed") ?? 0,
                    NBulletNumber = attackNode.ResolveInt("bulletNo") ?? 0,
                    BDeadlyAttack = (attackNode.ResolveInt("deadlyAttack") ?? 0) != 0,
                    BTremble = (attackNode.ResolveInt("tremble") ?? 0) != 0,
                    BDoFirst = (attackNode.ResolveInt("doFirst") ?? 0) != 0,
                    NMPBurn = attackNode.ResolveInt("MPBurn") ?? 0,
                    BKnockBack = (attackNode.ResolveInt("knockBack") ?? 0) != 0,
                    TRandDelayAttack = attackNode.ResolveInt("randDelayAttack") ?? 0,
                    BRush = (attackNode.ResolveInt("rush") ?? 0) != 0,
                    TAttackAfter = attackNode.ResolveInt("attackAfter") ?? 0,
                }
            );
        }
        Attacks = attacks.ToImmutableArray();

        // Build skill info array (skill/0, skill/1, ...)
        var skills = new List<MobSkillEntry>();
        var skillNode = node.ResolvePath("skill");
        if (skillNode != null)
        {
            foreach (var sk in skillNode.Children)
            {
                skills.Add(
                    new MobSkillEntry
                    {
                        NSkillID = sk.ResolveInt("skill") ?? 0,
                        NSLV = sk.ResolveInt("level") ?? 0,
                        NAction = sk.ResolveInt("action") ?? 0,
                        TEffectAfter = sk.ResolveInt("effectAfter") ?? 0,
                    }
                );
            }
        }
        Skills = skills.ToImmutableArray();
    }

    public int ID { get; }

    public MoveAbilityType MoveAbility { get; }

    public short Level { get; }

    public bool IsBoss { get; }

    public int MaxHP { get; }
    public int MaxMP { get; }

    public int PAD { get; }
    public int PDD { get; }
    public int PDR { get; }
    public int MAD { get; }
    public int MDD { get; }
    public int MDR { get; }
    public int ACC { get; }
    public int EVA { get; }

    public int EXP { get; }

    public IDictionary<Element, ElementAttribute> ElementAttributes { get; }

    // ── CRC fields (§11 in V95_CRC_Complete_Reference.md) ─────────────────

    public int Speed { get; }
    public int FlySpeed { get; }
    public int ChaseSpeed { get; }
    public bool BodyAttack { get; }
    public bool OnlyNormalAttack { get; }
    public bool NotAttack { get; }

    /// <summary>Mob self-destructs on death (distinct from skill SelfDestruction which is damage).</summary>
    public bool SelfDestructionMob { get; }
    public bool PickUpDrop { get; }
    public int EscortType { get; }
    public int HPRecovery { get; }
    public int MPRecovery { get; }
    public bool FirstAttack { get; }
    public bool Invincible { get; }
    public int FixedDamage { get; }
    public int PushedDamage { get; }

    /// <summary>Float speed coefficient used in mob physics (WZ: fs).</summary>
    public double MobFs { get; }

    public ImmutableArray<MobAttackEntry> Attacks { get; }
    public ImmutableArray<MobSkillEntry> Skills { get; }
}
