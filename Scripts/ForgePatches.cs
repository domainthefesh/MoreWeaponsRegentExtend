using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MoreWeaponsRegentExtend.Scripts.Cards;
using MoreWeaponsRegentExtend.Scripts.Powers;
using STS2RitsuLib.Patching.Models;

namespace MoreWeaponsRegentExtend.Scripts;

// 补丁：按本局兵器库选择创建武器，且不重复创建原版 SovereignBlade。
public class ForgeRandomizePatch : IPatchMethod
{
    public static string PatchId => "more_weapons_forge_randomize";
    public static string Description => "Create the run's chosen forgeable weapon, prevent duplicate SovereignBlade";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(ForgeCmd), nameof(ForgeCmd.Forge))];

    public static bool Prefix(
        decimal amount,
        Player player,
        AbstractModel? source,
        ref Task<IEnumerable<SovereignBlade>> __result)
        => !TryHandleForge(amount, player, source, out __result);

    // Both the public wrapper and its async entry use this route. The latter also
    // covers callers that inlined the wrapper before Harmony installed its patch.
    public static bool TryHandleForge(
        decimal amount,
        Player player,
        AbstractModel? source,
        out Task<IEnumerable<SovereignBlade>> result)
    {
        result = null!;
        if (CombatManager.Instance.IsOverOrEnding)
            return false;

        // The familiar route deliberately keeps the original game's blade creation,
        // forge scaling and support effects, including the protected async entry.
        if (WeaponSelection.GetWeapon(player) == WeaponKind.SovereignBlade)
            return false;

        bool hasSovereignBlade = player.PlayerCombatState!.AllCards
            .Any(c => !c.IsDupe && c.Pile?.Type != PileType.Exhaust && c is SovereignBlade);
        bool hasModForgeCard = player.PlayerCombatState!.AllCards
            .Any(c => !c.IsDupe && c.Pile?.Type != PileType.Exhaust && IsModForgeCard(c));
        bool hasKingGun = player.Creature.HasPower<KingGunPower>();

        bool everyForgeEnabled = WeaponSelection.IsEveryForgeRandom(player);

        // 王之枪激活时：铸造成子弹
        if (hasKingGun && !hasSovereignBlade)
        {
            if (!hasModForgeCard || everyForgeEnabled)
            {
                result = CreateBulletsAsync(amount, player, source);
                return true;
            }
            // 已有模组武器，跳过 Blade 创建
            result = ApplyForgeToModCardsAsync(amount, player, source);
            return true;
        }

        // 百武皆通：每次铸造重新随机生成一种武器。
        if (everyForgeEnabled && !hasSovereignBlade)
        {
            return CreateChosenWeapon(ref result, amount, player, source);
        }

        // 固定兵器：没有可继续铸造的武器时，生成本局选定兵器。
        // 更新前的存档没有开局选择，仍采用首次铸造随机的兼容行为。
        if (!hasSovereignBlade && !hasModForgeCard)
        {
            return CreateChosenWeapon(ref result, amount, player, source);
        }

        // 有模组卡但无原版 Blade：跳过 Blade 创建
        if (!hasSovereignBlade && hasModForgeCard)
        {
            result = ApplyForgeToModCardsAsync(amount, player, source);
            return true;
        }

        return false;
    }

    private static async Task<IEnumerable<SovereignBlade>> CreateBulletsAsync(
        decimal amount, Player player, AbstractModel? source)
    {
        var combatState = player.Creature.CombatState!;
        var bullet = combatState.CreateCard<Bullet>(player);
        bullet.CreatedThroughForge = true;

        await CardPileCmd.AddGeneratedCardToCombat(bullet, PileType.Hand, player);
        await Hook.AfterForge(combatState, amount, player, source);
        return Array.Empty<SovereignBlade>();
    }

    private static bool CreateChosenWeapon(
        ref Task<IEnumerable<SovereignBlade>> __result,
        decimal amount, Player player, AbstractModel? source)
    {
        var weapon = WeaponSelection.GetWeapon(player) ?? WeaponSelection.RollWeapon(player);
        switch (weapon)
        {
            case WeaponKind.SovereignBludgeon:
                __result = CreateCardAsync<SovereignBludgeon>(amount, player, source);
                break;
            case WeaponKind.ApocalypseLongbow:
                __result = CreateCardAsync<ApocalypseLongbow>(amount, player, source);
                break;
            case WeaponKind.SovereignCrystalBlade:
                __result = CreateCardAsync<SovereignCrystalBlade>(amount, player, source);
                break;
            case WeaponKind.SovereignShield:
                __result = CreateCardAsync<SovereignShield>(amount, player, source);
                break;
            case WeaponKind.SovereignSpear:
                __result = CreateCardAsync<SovereignSpear>(amount, player, source);
                break;
            case WeaponKind.SovereignAxe:
                __result = CreateCardAsync<SovereignAxe>(amount, player, source);
                break;
            case WeaponKind.SovereignGun:
                __result = CreateCardAsync<SovereignGun>(amount, player, source);
                break;
            case WeaponKind.SovereignScythe:
                __result = CreateCardAsync<SovereignScythe>(amount, player, source);
                break;
            case WeaponKind.NeptuneTrident:
                __result = CreateCardAsync<NeptuneTrident>(amount, player, source);
                break;
            case WeaponKind.SovereignKatana:
                __result = CreateCardAsync<SovereignKatana>(amount, player, source);
                break;
            case WeaponKind.SovereignWings:
                __result = CreateCardAsync<SovereignWings>(amount, player, source);
                break;
            case WeaponKind.SovereignDagger:
                __result = CreateCardAsync<SovereignDagger>(amount, player, source);
                break;
            case WeaponKind.SeaCalmingStaff:
                __result = CreateCardAsync<SeaCalmingStaff>(amount, player, source);
                break;
            case WeaponKind.GalaxyTrajectoryCannon:
                __result = CreateCardAsync<GalaxyTrajectoryCannon>(amount, player, source);
                break;
            case WeaponKind.HolyCodex:
                __result = CreateCardAsync<HolyCodex>(amount, player, source);
                break;
            case WeaponKind.RoyalBrassKnuckles:
                __result = CreateCardAsync<RoyalBrassKnuckles>(amount, player, source);
                break;
            default:
                return false;
        }
        return true;
    }

    private static async Task<IEnumerable<SovereignBlade>> CreateCardAsync<TCard>(
        decimal amount, Player player, AbstractModel? source)
        where TCard : CardModel
    {
        var combatState = player.Creature.CombatState!;
        var card = combatState.CreateCard<TCard>(player);
        SetCreatedThroughForge(card, true);

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);

        // 初次生成与后续铸造都由 ForgeSingleton 结算一次。
        await Hook.AfterForge(combatState, amount, player, source);

        return Array.Empty<SovereignBlade>();
    }

    private static async Task<IEnumerable<SovereignBlade>> ApplyForgeToModCardsAsync(
        decimal amount, Player player, AbstractModel? source)
    {
        var combatState = player.Creature.CombatState!;
        await Hook.AfterForge(combatState, amount, player, source);
        return Array.Empty<SovereignBlade>();
    }

    private static void SetCreatedThroughForge(CardModel card, bool value)
    {
        if (card is SovereignBludgeon bludgeon)
            bludgeon.CreatedThroughForge = value;
        else if (card is ApocalypseLongbow longbow)
            longbow.CreatedThroughForge = value;
        else if (card is SovereignCrystalBlade crystalBlade)
            crystalBlade.CreatedThroughForge = value;
        else if (card is SovereignShield shield)
            shield.CreatedThroughForge = value;
        else if (card is SovereignSpear spear)
            spear.CreatedThroughForge = value;
        else if (card is SovereignAxe axe)
            axe.CreatedThroughForge = value;
        else if (card is SovereignScythe scythe)
            scythe.CreatedThroughForge = value;
        else if (card is SovereignGun gun)
            gun.CreatedThroughForge = value;
    }

    public static bool IsModForgeCard(CardModel card)
    {
        return card is SovereignBludgeon
            || card is ApocalypseLongbow
            || card is SovereignCrystalBlade
            || card is CrystalDagger
            || card is SovereignShield
            || card is SovereignSpear
            || card is SovereignAxe
            || card is SovereignGun
            || card is SovereignScythe
            || card is ForgeableWeaponCardBase;
    }

    // 原版君王之剑的联动也作用于附属武器；子弹仍不阻止铸造生成主武器。
    public static bool IsBladeCompatibleCard(CardModel card) => IsModForgeCard(card) || card is Bullet;
}

// 补丁：ConquerorPower 双倍伤害也对模组锻造卡生效
public class ConquerorModPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_conqueror_mod";
    public static string Description => "ConquerorPower also doubles damage of mod forge cards";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(ConquerorPower), nameof(ConquerorPower.ModifyDamageMultiplicative))];

    public static bool Prefix(
        ConquerorPower __instance,
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        ref decimal __result)
    {
        if (cardSource == null || !ForgeRandomizePatch.IsBladeCompatibleCard(cardSource))
            return true;

        if (!props.IsPoweredAttack())
            return true;

        if (target != __instance.Owner)
            return true;

        __result = 2m;
        return false;
    }
}

// 补丁：SwordSagePower 重放次数也对模组锻造卡生效
public class SwordSageModPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_sword_sage_mod";
    public static string Description => "SwordSagePower also grants replays to mod forge cards";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(SwordSagePower), nameof(SwordSagePower.AfterPowerAmountChanged))];

    public static void Postfix(
        SwordSagePower __instance,
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not SwordSagePower || power.Owner != __instance.Owner)
            return;

        var cards = __instance.Owner?.Player?.PlayerCombatState?.AllCards ?? Array.Empty<CardModel>();
        foreach (var card in cards)
            TryAddReplayToModForgeCard(__instance, card, (int)amount);
    }

    public static bool TryAddReplayToModForgeCard(SwordSagePower instance, CardModel card, int amount)
    {
        if (card.Owner != instance.Owner?.Player)
            return false;

        if (!ForgeRandomizePatch.IsBladeCompatibleCard(card))
            return false;

        card.BaseReplayCount += amount;
        return true;
    }
}

// 补丁：SwordSagePower 已存在时，新进入战斗的模组锻造卡也获得重放次数
public class SwordSageCardEnteredModPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_sword_sage_card_entered_mod";
    public static string Description => "SwordSagePower grants replays to mod forge cards entering combat";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(SwordSagePower), nameof(SwordSagePower.AfterCardEnteredCombat))];

    public static void Postfix(SwordSagePower __instance, CardModel card)
    {
        if (card.IsClone)
            return;

        SwordSageModPatch.TryAddReplayToModForgeCard(__instance, card, __instance.Amount);
    }
}

// 补丁：SwordSagePower 移除时同步扣回模组锻造卡的重放次数
public class SwordSageRemovedModPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_sword_sage_removed_mod";
    public static string Description => "SwordSagePower removes replays from mod forge cards when removed";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(SwordSagePower), nameof(SwordSagePower.AfterRemoved))];

    public static void Postfix(SwordSagePower __instance, Creature oldOwner)
    {
        var cards = oldOwner.Player?.PlayerCombatState?.AllCards ?? Array.Empty<CardModel>();
        foreach (var card in cards)
        {
            if (ForgeRandomizePatch.IsBladeCompatibleCard(card))
                card.BaseReplayCount -= __instance.Amount;
        }
    }
}

// 补丁：SummonForth 也将模组锻造卡牌移回手牌
public class SummonForthModPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_summon_forth_mod";
    public static string Description => "SummonForth also retrieves mod forge cards to hand";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(SummonForth), "OnPlay", new Type[] { typeof(PlayerChoiceContext), typeof(CardPlay) })];

    public static bool Prefix(
        SummonForth __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result)
    {
        __result = OnPlayReplacement(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task OnPlayReplacement(SummonForth instance, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = instance.Owner;
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 找原版 SovereignBlade + 模组锻造卡（非手牌堆）
        var cards = player.PlayerCombatState!.AllCards
            .Where(c => !c.IsDupe
                && (c is SovereignBlade || ForgeRandomizePatch.IsBladeCompatibleCard(c))
                && (c.Pile == null || c.Pile.Type != PileType.Hand))
            .ToList();

        if (cards.Count > 0)
            await CardPileCmd.Add(cards, PileType.Hand);

        await ForgeCmd.Forge(instance.DynamicVars.Forge.IntValue, player, instance);
    }
}

// 补丁：铸造及君王之剑联动卡牌只预览本局选择的一把武器。
public class ParryHoverModPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_parry_hover_mod";
    public static string Description => "Forge and blade support cards preview only the run's chosen weapon";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardModel), "get_HoverTips")];

    public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        __result = WeaponHoverTips.ForCard(__instance, __result);
    }
}
