using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MoreWeaponsRegentExtend.Scripts.Cards;
using STS2RitsuLib.Patching.Models;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

public static class WeaponSpecialization
{
    // Auxiliary cards keep their own costs; specialization belongs to the actual weapon.
    public static bool IsMainWeapon(CardModel card) => card is SovereignBlade ||
        card is MoreWeaponsCardBase && card is not (HandyWeapon or Bullet or CrystalDagger or CannonCharge);

    public static int GetBaseCost(int originalCost, CardModel card)
    {
        if (originalCost < 0 || !card.IsMutable || card.Owner == null || card.EnergyCost.CostsX ||
            !IsMainWeapon(card))
            return originalCost;
        return Math.Max(0, originalCost + (WeaponRewardRuntime.Has(card, 5) ? 1 :
            WeaponRewardRuntime.Has(card, 6) ? -1 : 0));
    }

    public static decimal GetDamageMultiplier(CardModel card) => card.IsMutable && card.Owner != null && IsMainWeapon(card)
        ? GetDamageMultiplier(card.Owner, WeaponProgression.GetWeaponKind(card)) : 1m;

    public static decimal GetDamageMultiplier(Player player, WeaponKind? kind)
    {
        if (kind == null)
            return 1m;
        if (WeaponProgression.HasReward(player, kind.Value, 5))
            return 1.5m;
        return WeaponProgression.HasReward(player, kind.Value, 6) ? 0.7m : 1m;
    }
}

/// <summary>Adjust the intrinsic cost before native local overrides and global effects.</summary>
public sealed class WeaponSpecializationCostPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_specialization_base_cost";
    public static string Description => "Apply weapon specialization to base cost before local cost modifiers";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardEnergyCost), nameof(CardEnergyCost.GetWithModifiers), [typeof(CostModifiers)])];

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var body = instructions.ToList();
        var baseField = AccessTools.Field(typeof(CardEnergyCost), "_base");
        var cardField = AccessTools.Field(typeof(CardEnergyCost), "_card");
        if (baseField?.FieldType != typeof(int) || cardField?.FieldType != typeof(CardModel))
            throw new InvalidOperationException("CardEnergyCost fields changed; cannot adjust intrinsic costs safely.");
        var firstBaseRead = body.FindIndex(instruction => instruction.LoadsField(baseField));
        if (firstBaseRead < 0)
            throw new InvalidOperationException("CardEnergyCost base read was not found.");
        body.InsertRange(firstBaseRead + 1,
        [
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, cardField),
            new CodeInstruction(OpCodes.Call, typeof(WeaponSpecialization).GetMethod(nameof(WeaponSpecialization.GetBaseCost))!)
        ]);
        return body;
    }
}
