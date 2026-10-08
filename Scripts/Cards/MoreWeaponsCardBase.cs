using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

// 抽象基类：统一管理卡图路径，所有子类自动注册到 TokenCardPool
[RegisterCard(typeof(TokenCardPool), Inherit = true)]
public abstract class MoreWeaponsCardBase : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://MoreWeaponsRegentExtend/images/cards/{GetType().Name}.png"
    );

    protected override void AddExtraArgsToDescription(LocString description)
    {
        WeaponRewardRuntime.Sync(this);
        base.AddExtraArgsToDescription(description);
        description.Add("HasParry", GetOwnerParryAmount(this) > 0m);
        description.Add("Reward1", WeaponRewardRuntime.Has(this, 1));
        description.Add("Reward2", WeaponRewardRuntime.Has(this, 2));
        description.Add("Reward3", WeaponRewardRuntime.Has(this, 3));
        description.Add("FirstAvailable", WeaponRewardRuntime.IsFirstAvailable(this));
    }

    protected MoreWeaponsCardBase(int energyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary)
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // === ParryPower 格挡计算（子类共用） ===
    protected static decimal GetOwnerParryAmount(CardModel card)
    {
        if (!card.IsMutable || card.Owner == null || card.Pile == null || !card.Pile.IsCombatPile)
            return 0m;
        var amount = card.Owner.Creature.GetPowerAmount<ParryPower>();
        return amount;
    }

    protected static IEnumerable<DynamicVar> ParryVars() =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => GetOwnerParryAmount(card))
    ];

    // 与原版剑一致：每次实际出牌独立获得格挡，交给原版处理敏捷、脆弱等修正。
    protected Task GainParryBlock(CardPlay cardPlay) => GetOwnerParryAmount(this) > 0m
        ? CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(cardPlay.Target),
            DynamicVars.CalculatedBlock.Props, cardPlay)
        : Task.CompletedTask;

    // === VFX 清理：尝试移除浮空剑 VFX ===
    protected void RemoveVfxIfAny()
    {
        if (IsDupe)
            return;
        NWeaponVfx.RemoveFor(this);

        try
        {
            if (Owner != null)
                SovereignBlade.GetVfxNode(Owner, this)?.RemoveSovereignBlade();
        }
        catch { /* VFX 清理失败不抛异常 */ }
    }

    protected NWeaponVfx? EnsureWeaponVfx(string sceneName) => NWeaponVfx.EnsureAttached(this, sceneName);

    protected void RemoveWeaponVfx() => NWeaponVfx.RemoveFor(this);

    protected Task TriggerWeaponAttack(Creature? target)
    {
        NWeaponVfx.AttackFor(this, target);
        NUpdateCardVfx.Play(this, target);
        return Task.CompletedTask;
    }

    protected Task TriggerWeaponAttackFirstEnemy()
    {
        var enemies = CombatState?.HittableEnemies;
        if (enemies != null && enemies.Count > 0)
        {
            NWeaponVfx.AttackFor(this, enemies[0]);
            foreach (var enemy in enemies)
                NUpdateCardVfx.Play(this, enemy);
        }

        return Task.CompletedTask;
    }

    // 群体卡的 cardPlay.Target 为空；附加效果跟随实际受击者。
    protected static IReadOnlyList<Creature> GetSurvivingAttackTargets(AttackCommand attack) =>
        attack.Results.SelectMany(results => results)
            .Select(result => result.Receiver).Distinct()
            .Where(target => target.IsAlive).ToArray();
}
