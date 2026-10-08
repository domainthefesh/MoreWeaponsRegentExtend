using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MoreWeaponsRegentExtend.Scripts.Cards;
using STS2RitsuLib.Patching.Models;

namespace MoreWeaponsRegentExtend.Scripts;

public sealed class CannonAutoPlayGatePatch : IPatchMethod
{
    public static string PatchId => "more_weapons_cannon_auto_play_gate";
    public static string Description => "Cold trajectory cannons cannot be auto-played or discarded by failed auto-play";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() => [new(typeof(CardCmd), nameof(CardCmd.AutoPlay))];

    public static bool Prefix(CardModel card, ref Task __result) => CannonPlayGate.Allow(card, ref __result);
}

public sealed class CannonPlayWrapperGatePatch : IPatchMethod
{
    public static string PatchId => "more_weapons_cannon_play_wrapper_gate";
    public static string Description => "Reject all cold cannon wrapper executions before play effects and hooks";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() => [new(typeof(CardModel), nameof(CardModel.OnPlayWrapper))];

    public static bool Prefix(CardModel __instance, ref Task __result) => CannonPlayGate.Allow(__instance, ref __result);
}

internal static class CannonPlayGate
{
    public static bool Allow(CardModel card, ref Task result)
    {
        if (card is not GalaxyTrajectoryCannon cannon)
            return true;
        if (cannon.IsHot)
        {
            // CreateDupe sets its dupe flag after AfterCloned. Synchronize its
            // keyword presentation now that it can resolve the original cannon.
            cannon.RefreshThermalState();
            return true;
        }
        result = Task.CompletedTask;
        return false;
    }
}
