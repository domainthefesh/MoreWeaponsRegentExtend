using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MoreWeaponsRegentExtend.Scripts.Cards;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Enchantments;

public static class WeaponTaskEnchantments
{
    public sealed record GunShotEffects(bool HasFortune, int FortuneDamagePercent, bool HasResentment);

    public static bool IsPrimaryWeapon(CardModel card)
        => card is not (Bullet or CrystalDagger or CannonCharge) && WeaponProgression.GetWeaponKind(card) != null;

    /// <summary>Only call for a newly generated weapon from Forge, before it enters the hand.</summary>
    public static Task ApplyOnForge(CardModel card)
    {
        if (!card.IsMutable || card.IsDupe || card.Owner == null || !IsPrimaryWeapon(card) ||
            WeaponProgression.GetWeaponKind(card) is not { } kind)
            return Task.CompletedTask;
        if (WeaponProgression.HasReward(card.Owner, kind, 7))
            AddWithoutReplacing<PerfectFit>(card);
        if (WeaponProgression.HasReward(card.Owner, kind, 8))
        {
            AddWithoutReplacing<StrongFortune>(card);
            Find<StrongFortune>(card)?.RollForCurrentTurn();
        }
        if (WeaponProgression.HasReward(card.Owner, kind, 9))
            AddWithoutReplacing<Resentment>(card);
        return Task.CompletedTask;
    }

    public static T? Find<T>(CardModel card) where T : EnchantmentModel
        => card.Enchantment is T single ? single :
           card.Enchantment is TemperedEnchantments combined ? combined.Children.OfType<T>().FirstOrDefault() : null;

    public static GunShotEffects GetGunShotEffects(CardModel card)
    {
        var fortune = Find<StrongFortune>(card);
        return new(fortune != null, fortune?.DamagePercent ?? 0, Find<Resentment>(card) != null);
    }

    public static int RollFortune(Player player)
        => player.RunState.Rng.CombatCardSelection.NextInt(-10, 51);

    public static decimal GetGunShotMultiplier(bool strongFortune, int fortunePercent, bool resentment)
        => (strongFortune ? 1m + Math.Clamp(fortunePercent, -10, 50) / 100m : 1m) * (resentment ? 2m : 1m);

    private static void AddWithoutReplacing<T>(CardModel card) where T : EnchantmentModel
    {
        if (Find<T>(card) != null)
            return;
        var added = ModelDb.Enchantment<T>().ToMutable();
        added.Amount = 1;
        if (card.Enchantment == null)
        {
            CardCmd.Enchant(added, card, 1m);
            return;
        }
        if (card.Enchantment is TemperedEnchantments combined)
        {
            combined.AddChild(added);
            return;
        }

        var original = card.Enchantment;
        card.ClearEnchantmentInternal();
        var container = (TemperedEnchantments)ModelDb.Enchantment<TemperedEnchantments>().ToMutable();
        container.SetAlreadyAppliedChildren([original]);
        CardCmd.Enchant(container, card, 1m);
        container.AddChild(added);
    }
}
