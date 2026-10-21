using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MoreWeaponsRegentExtend.Scripts.Enchantments;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

/// <summary>Creation-only rewards never reapply when an existing weapon is forged.</summary>
public static class WeaponForgeRewards
{
    private sealed class Creation { public bool Applied; }
    private static readonly ConditionalWeakTable<CardModel, Creation> Creations = new();

    public static async Task ApplyOnCreation(CardModel card)
    {
        if (!card.IsMutable || card.IsDupe || card.Owner == null || !WeaponSpecialization.IsMainWeapon(card))
            return;
        var creation = Creations.GetOrCreateValue(card);
        if (creation.Applied)
            return;
        // Mark before upgrade/enchantment hooks can reenter card generation.
        creation.Applied = true;
        if (WeaponRewardRuntime.Has(card, 4) && card.IsUpgradable)
            CardCmd.Upgrade(card);
        await WeaponTaskEnchantments.ApplyOnForge(card);
    }
}
