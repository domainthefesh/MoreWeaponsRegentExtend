using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MoreWeaponsRegentExtend.Scripts.Progression;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MoreWeaponsRegentExtend.Scripts.Cards;
using MoreWeaponsRegentExtend.Scripts.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace MoreWeaponsRegentExtend.Scripts;

// 铸造单例：监听 AfterForge 事件，为所有锻造卡牌增加数值并播放铸造动画
[RegisterSingleton]
public class ForgeSingleton : HookedSingletonModel
{
    // 全局累计锻造数值（供 KingGunPower 等参考）
    public static decimal TotalForgedAmount { get; private set; }

    public ForgeSingleton() : base(HookType.Combat) { }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        WeaponRewardRuntime.Sync(cardPlay.Card);
        WeaponRewardRuntime.BeginPlay(cardPlay);
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        WeaponRewardRuntime.EndPlay(cardPlay);
        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        WeaponRewardRuntime.Sync(card);
        return Task.CompletedTask;
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
        => AfterCardEnteredCombat(card);

    public override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay,
        ResourceInfo resources, CardLocation cardLocation)
        => card is SeaCalmingStaff && WeaponRewardRuntime.Has(card, 1) && WeaponRewardRuntime.IsFirstAvailable(card)
            ? new CardLocation(card.Owner, PileType.Hand, CardPilePosition.Bottom) : cardLocation;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card is not (SovereignGun or GalaxyTrajectoryCannon or SovereignBlade) || !WeaponRewardRuntime.Has(card, 1))
            return false;
        modifiedCost = System.Math.Max(0m, originalCost - 1m);
        return modifiedCost != originalCost;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (cardSource == null || !props.IsPoweredAttack())
            return 0m;
        return WeaponRewardRuntime.GetBaseAttackBonus(cardSource);
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!props.IsPoweredAttack() || cardSource == null)
            return 1m;
        if (cardSource is SovereignKatana && WeaponRewardRuntime.Has(cardSource, 1) &&
            WeaponRewardRuntime.IsFirstDamage(cardSource, cardPlay))
            return 1.5m;
        if (cardSource is SovereignDagger && WeaponRewardRuntime.Has(cardSource, 2) &&
            (cardSource.Owner.Gold > 300 || (target != null && target.Monster?.IntendsToAttack != true)))
            return 3m;
        return 1m;
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        BladeFatalGrowth.Capture(command);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        NWeaponVfx.RemoveAll();
        return Task.CompletedTask;
    }

    public override async Task AfterForge(decimal amount, Player forger, AbstractModel? source)
    {
        TotalForgedAmount += amount;

        foreach (var card in forger.PlayerCombatState!.AllCards)
        {
            // 重放用的临时副本不是独立武器，不能重复累计铸造。
            if (card.IsDupe)
                continue;

            if (card is ForgeableWeaponCardBase newWeapon)
            {
                newWeapon.AddForge(amount);
            }
            else if (card is SovereignBludgeon bludgeon)
            {
                bludgeon.AddDamage(amount);
                bludgeon.AfterForged();
                // 使用专属 VFX，不走 PlayCombatRoomForgeVfx
            }
            else if (card is ApocalypseLongbow longbow)
            {
                longbow.AddDamage(amount);
                longbow.AfterForged();
            }
            else if (card is SovereignCrystalBlade crystalBlade)
            {
                crystalBlade.AddDamage(amount);
                crystalBlade.AfterForged();
            }
            else if (card is CrystalDagger dagger)
            {
                dagger.AddDamage(amount);
                dagger.AfterForged();
                // 水晶小刀现在使用统一的原生武器场景，铸造不再生成原版浮剑。
            }
            else if (card is SovereignShield shield)
            {
                shield.AddDamage(amount);
                shield.AfterForged();
            }
            else if (card is SovereignSpear spear)
            {
                spear.AddDamage(amount);
                spear.AfterForged();
            }
            else if (card is SovereignAxe axe)
            {
                axe.AddDamage(amount);
                axe.AfterForged();
            }
            else if (card is SovereignScythe scythe)
            {
                scythe.AddDamage(amount);
                scythe.AfterForged();
            }
            else if (card is Bullet bullet)
            {
                bullet.AddDamage(amount);
                bullet.AfterForged();
            }
        }

        // 铸造增加王之枪层数
        var gunPower = forger.Creature.GetPower<KingGunPower>();
        if (gunPower != null)
        {
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), gunPower, amount, null, null);
        }

        // 卡牌锻造闪光特效（NCardSmithVfx）
        PlayCardSmithVfx(forger);
    }

    private static void PlayCardSmithVfx(Player forger)
    {
        var modCards = forger.PlayerCombatState!.AllCards
            .Where(c => !c.IsDupe && ForgeRandomizePatch.IsModForgeCard(c))
            .ToList();

        if (modCards.Count == 0) return;

        // 手牌中的卡牌：逐张播放闪光
        var handCards = modCards.Where(c => c.Pile?.Type == PileType.Hand).ToList();
        foreach (var card in handCards)
        {
            var nCard = NCombatRoom.Instance?.Ui?.Hand?.GetCard(card);
            if (nCard != null)
            {
                var vfx = NCardSmithVfx.Create(nCard, playSfx: false);
                NRun.Instance?.GlobalUi?.AboveTopBarVfxContainer?.AddChildSafely(vfx);
            }
        }

        // 非手牌中的卡牌：预览闪光
        var otherCards = modCards.Where(c => c.Pile?.Type != PileType.Hand).ToList();
        if (otherCards.Count > 0)
        {
            var vfx = NCardSmithVfx.Create(otherCards, playSfx: false);
            NRun.Instance?.GlobalUi?.CardPreviewContainer?.AddChildSafely(vfx);
        }
    }
}
