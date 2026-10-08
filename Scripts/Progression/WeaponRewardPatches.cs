using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

public sealed class WeaponRewardDescriptionPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_reward_description";
    public static string Description => "Refresh claimed weapon rewards before card description previews";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), [typeof(PileType), typeof(Creature)])];

    public static void Prefix(CardModel __instance, Creature? __1)
    {
        WeaponRewardRuntime.Sync(__instance);
        // Native card views normally update previews first. Direct inspections and
        // already-existing cards after a claim must also show the intrinsic bonus.
        if (WeaponRewardRuntime.GetBaseAttackBonus(__instance) != 0m)
            __instance.UpdateDynamicVarPreview(CardPreviewMode.Normal, __1, __instance.DynamicVars);
    }
}

/// <summary>
/// Native non-hand and out-of-combat previews intentionally omit global combat
/// hooks. Show the weapon's own run rewards there without mutating its base vars.
/// Combat previews already obtain the same bonus through ModifyDamageAdditive.
/// </summary>
public sealed class WeaponRewardDamagePreviewPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_reward_damage_preview";
    public static string Description => "Show intrinsic weapon rewards in noncombat damage previews";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(DamageVar), nameof(DamageVar.UpdateCardPreview),
            [typeof(CardModel), typeof(CardPreviewMode), typeof(Creature), typeof(bool)]),
        new(typeof(CalculatedDamageVar), nameof(CalculatedDamageVar.UpdateCardPreview),
            [typeof(CardModel), typeof(CardPreviewMode), typeof(Creature), typeof(bool)])
    ];

    public static void Postfix(DynamicVar __instance, CardModel __0, bool __3)
    {
        if (!__3)
            __instance.PreviewValue += WeaponRewardRuntime.GetBaseAttackBonus(__0);
    }
}
