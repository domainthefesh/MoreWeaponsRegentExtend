using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace MoreWeaponsRegentExtend.Scripts.Enchantments;

internal static class TemperedEnchantmentHooks
{
    public static IEnumerable<AbstractModel> Expand(IEnumerable<AbstractModel> listeners)
    {
        foreach (var model in listeners)
        {
            // Replace, rather than retain, the container: RunState may traverse
            // an already expanded CombatState and must not fire children twice.
            if (model is TemperedEnchantments combined)
            {
                foreach (var child in combined.Children)
                    yield return child;
            }
            else
                yield return model;
        }
    }
}

public sealed class TemperedCombatHooksPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_tempered_combat_hooks";
    public static string Description => "Retain every combined weapon enchantment's combat hooks";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() => [new(typeof(CombatState), nameof(CombatState.IterateHookListeners), [])];
    public static void Postfix(ref IEnumerable<AbstractModel> __result)
        => __result = TemperedEnchantmentHooks.Expand(__result);
}

public sealed class TemperedRunHooksPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_tempered_run_hooks";
    public static string Description => "Retain every combined weapon enchantment's run hooks";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RunState), nameof(RunState.IterateHookListeners), [typeof(ICombatState)])];
    public static void Postfix(ref IEnumerable<AbstractModel> __result)
        => __result = TemperedEnchantmentHooks.Expand(__result);
}

public sealed class TemperedHoverTipsPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_tempered_enchantment_hover_tips";
    public static string Description => "Show each weapon enchantment's real hover tip and icon";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(EnchantmentModel), "get_HoverTips", [])];
    public static void Postfix(EnchantmentModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance is TemperedEnchantments combined)
            __result = combined.Children.SelectMany(child => child.HoverTips).ToArray();
    }
}

public sealed class TemperedExtraCardTextPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_tempered_enchantment_card_text";
    public static string Description => "Show all combined enchantments in the weapon card description";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(EnchantmentModel), "get_DynamicExtraCardText", [])];
    public static void Postfix(EnchantmentModel __instance, ref LocString? __result)
    {
        if (__instance is not TemperedEnchantments combined)
            return;
        var texts = combined.Children.Select(child => child.DynamicExtraCardText?.GetFormattedText())
            .Where(text => !string.IsNullOrWhiteSpace(text)).ToArray();
        if (texts.Length == 0)
        {
            __result = null;
            return;
        }
        var text = new LocString("enchantments", combined.Id.Entry + ".extraCardText");
        text.Add("CombinedText", string.Join("\n", texts));
        __result = text;
    }
}
