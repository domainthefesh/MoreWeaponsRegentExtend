using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MoreWeaponsRegentExtend.Scripts.Powers;
using STS2RitsuLib.Patching.Models;

namespace MoreWeaponsRegentExtend.Scripts;

// A multiplicative power hook would run before some other multipliers/caps.
// Filter the completed damage calculation so Weak, Vulnerable and Intangible count consistently.
public sealed class HeavyPressureDamagePatch : IPatchMethod
{
    public static string PatchId => "more_weapons_heavy_pressure_damage";
    public static string Description => "Heavy Pressure suppresses outgoing damage below its threshold";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() => [new(typeof(Hook), nameof(Hook.ModifyDamage))];

    public static void Postfix(Creature? dealer, ModifyDamageHookType modifyDamageHookType,
        ref IEnumerable<AbstractModel> modifiers, ref decimal __result)
    {
        if (!modifyDamageHookType.HasFlag(ModifyDamageHookType.Cap) || __result <= 0m)
            return;
        var pressure = dealer?.GetPower<HeavyPressurePower>();
        if (pressure == null || pressure.Amount <= 0)
            return;
        decimal filtered = pressure.FilterDamage(__result);
        if (filtered == __result)
            return;
        __result = filtered;
        modifiers = modifiers.Append(pressure).Distinct();
    }
}
