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

// 补丁：首次锻造时随机创建一种君王武器，且不重复创建原版 SovereignBlade
public class ForgeRandomizePatch : IPatchMethod
{
    public static string PatchId => "more_weapons_forge_randomize";
    public static string Description => "Randomly create a forgeable weapon on first forge, prevent duplicate SovereignBlade";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(ForgeCmd), nameof(ForgeCmd.Forge))];

    private static readonly Random _rng = new();

    public static bool Prefix(
        decimal amount,
        Player player,
        AbstractModel? source,
        ref Task<IEnumerable<SovereignBlade>> __result)
    {
        if (CombatManager.Instance.IsOverOrEnding)
            return true;

        bool hasSovereignBlade = player.PlayerCombatState!.AllCards
            .Any(c => !c.IsDupe && c.Pile?.Type != PileType.Exhaust && c is SovereignBlade);
        bool hasModForgeCard = player.PlayerCombatState!.AllCards
            .Any(c => !c.IsDupe && c.Pile?.Type != PileType.Exhaust && IsModForgeCard(c));
        bool hasKingGun = player.Creature.HasPower<KingGunPower>();

        bool firstForgeEnabled = MoreWeaponsSettingsPage.FirstForgeRandom.Read();
        bool everyForgeEnabled = MoreWeaponsSettingsPage.EveryForgeRandom.Read();

        // 王之枪激活时：铸造成子弹
        if (hasKingGun && !hasSovereignBlade)
        {
            if (!hasModForgeCard || everyForgeEnabled)
            {
                __result = CreateBulletsAsync(amount, player, source);
                return false;
            }
            // 已有模组武器，跳过 Blade 创建
            __result = ApplyForgeToModCardsAsync(amount, player, source);
            return false;
        }

        // 每次铸造都随机生成（选项10）
        if (everyForgeEnabled && !hasSovereignBlade)
        {
            CreateRandomWeapon(ref __result, amount, player, source);
            return false;
        }

        // 首次铸造随机生成（选项1，默认勾选）
        if (firstForgeEnabled && !hasSovereignBlade && !hasModForgeCard)
        {
            CreateRandomWeapon(ref __result, amount, player, source);
            return false;
        }

        // 有模组卡但无原版 Blade：跳过 Blade 创建
        if (!hasSovereignBlade && hasModForgeCard)
        {
            __result = ApplyForgeToModCardsAsync(amount, player, source);
            return false;
        }

        return true;
    }

    private static async Task<IEnumerable<SovereignBlade>> CreateBulletsAsync(
        decimal amount, Player player, AbstractModel? source)
    {
        var combatState = player.Creature.CombatState!;
        var bullet = combatState.CreateCard<Bullet>(player);
        bullet.CreatedThroughForge = true;
        bullet.AddDamage(amount);
        bullet.AfterForged();

        await CardPileCmd.AddGeneratedCardToCombat(bullet, PileType.Hand, player);
        await Hook.AfterForge(combatState, amount, player, source);
        return Array.Empty<SovereignBlade>();
    }

    private static void CreateRandomWeapon(
        ref Task<IEnumerable<SovereignBlade>> __result,
        decimal amount, Player player, AbstractModel? source)
    {
        var enabled = MoreWeaponsSettingsPage.GetEnabledWeaponIndices();
        if (enabled.Count == 0)
        {
            return; // 没有启用的武器，走原版创建 SovereignBlade
        }

        int roll = _rng.Next(enabled.Count);
        switch (enabled[roll])
        {
            case 0:
                __result = CreateCardAsync<SovereignBludgeon>(amount, player, source);
                break;
            case 1:
                __result = CreateCardAsync<ApocalypseLongbow>(amount, player, source);
                break;
            case 2:
                __result = CreateCardAsync<SovereignCrystalBlade>(amount, player, source);
                break;
            case 3:
                __result = CreateCardAsync<SovereignShield>(amount, player, source);
                break;
            case 4:
                __result = CreateCardAsync<SovereignSpear>(amount, player, source);
                break;
            case 5:
                __result = CreateCardAsync<SovereignAxe>(amount, player, source);
                break;
            case 6:
                __result = CreateCardAsync<SovereignGun>(amount, player, source);
                break;
            case 7:
                __result = CreateCardAsync<SovereignScythe>(amount, player, source);
                break;
        }
    }

    private static async Task<IEnumerable<SovereignBlade>> CreateCardAsync<TCard>(
        decimal amount, Player player, AbstractModel? source)
        where TCard : CardModel
    {
        var combatState = player.Creature.CombatState!;
        var card = combatState.CreateCard<TCard>(player);
        SetCreatedThroughForge(card, true);

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);

        ApplyForgeAmount(card, amount);

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

    private static void ApplyForgeAmount(CardModel card, decimal amount)
    {
        if (card is SovereignBludgeon bludgeon)
        {
            bludgeon.AddDamage(amount);
            bludgeon.AfterForged();
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
        else if (card is SovereignGun gun)
        {
            gun.AfterForged();
        }
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
            || card is SovereignScythe;
    }
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
        if (cardSource == null || !ForgeRandomizePatch.IsModForgeCard(cardSource))
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

        if (!ForgeRandomizePatch.IsModForgeCard(card))
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
            if (ForgeRandomizePatch.IsModForgeCard(card))
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
                && (c is SovereignBlade || ForgeRandomizePatch.IsModForgeCard(c))
                && (c.Pile == null || c.Pile.Type != PileType.Hand))
            .ToList();

        if (cards.Count > 0)
            await CardPileCmd.Add(cards, PileType.Hand);

        await ForgeCmd.Forge(instance.DynamicVars.Forge.IntValue, player, instance);
    }
}

// 补丁：Parry 的 ExtraHoverTips 也显示模组锻造卡牌预览
public class ParryHoverModPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_parry_hover_mod";
    public static string Description => "Parry card also shows mod forge card previews in hover tips";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Parry), "get_ExtraHoverTips")];

    public static void Postfix(ref IEnumerable<IHoverTip> __result)
    {
        var extra = new List<IHoverTip>(__result);
        extra.Add(HoverTipFactory.FromCard<SovereignBludgeon>());
        extra.Add(HoverTipFactory.FromCard<ApocalypseLongbow>());
        extra.Add(HoverTipFactory.FromCard<SovereignCrystalBlade>());
        extra.Add(HoverTipFactory.FromCard<SovereignShield>());
        extra.Add(HoverTipFactory.FromCard<SovereignSpear>());
        extra.Add(HoverTipFactory.FromCard<SovereignAxe>());
        extra.Add(HoverTipFactory.FromCard<SovereignScythe>());
        __result = extra;
    }
}
