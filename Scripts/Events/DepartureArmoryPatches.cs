using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace MoreWeaponsRegentExtend.Scripts.Events;

internal static class DepartureArmoryFlow
{
    private sealed class Gate { public bool Queued; public bool EnteringNeow; }
    private static readonly ConditionalWeakTable<IRunState, Gate> Gates = new();

    internal static bool IsPendingNeow(RunManager manager) =>
        manager.IsInProgress && manager.DebugOnlyGetState()!.CurrentActIndex == 0 &&
        manager.DebugOnlyGetState()!.CurrentRoom is EventRoom { CanonicalEvent: Neow } &&
        manager.DebugOnlyGetState()!.Players.Any(player => player.Character is Regent && !WeaponSelection.IsChosen(player));

    internal static void QueueAfterNeow(RunManager manager)
    {
        if (!IsPendingNeow(manager) || manager.EventSynchronizer.Events.Any(e => !e.IsFinished))
            return;
        var state = manager.DebugOnlyGetState()!;
        var gate = Gates.GetValue(state, static _ => new Gate());
        if (gate.Queued || gate.EnteringNeow)
            return;
        gate.Queued = true;
        TaskHelper.RunSafely(EnterAfterNeow(manager, state, gate));
    }

    internal static void SetEnteringNeow(RunManager manager, bool entering) =>
        Gates.GetValue(manager.DebugOnlyGetState()!, static _ => new Gate()).EnteringNeow = entering;

    private static async Task EnterAfterNeow(RunManager manager, IRunState state, Gate gate)
    {
        try
        {
            // StateChanged runs inside the option task. Yield before waiting on that task or exiting.
            await Task.Yield();
            await manager.EventSynchronizer.AwaitPendingOptionTasks();
            if (SaveManager.Instance.CurrentRunSaveTask is { } saveTask)
                await saveTask;
            if (!ReferenceEquals(manager.DebugOnlyGetState()!, state) || !IsPendingNeow(manager))
                return;
            NMapScreen.Instance?.Close();
            NMapScreen.Instance?.SetTravelEnabled(false);
            var room = new EventRoom(ModelDb.Event<DepartureArmory>());
            await manager.EnterRoom(room);
            await SaveManager.Instance.SaveRun(room);
        }
        catch
        {
            gate.Queued = false;
            throw;
        }
    }
}

public sealed class DepartureArmoryAfterNeowPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_armory_after_neow";
    public static string Description => "Open the departure armory after all Neow choices finish";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() => [new(typeof(EventRoom), "OnEventStateChanged")];
    public static void Postfix(EventRoom __instance)
    {
        if (__instance.CanonicalEvent is Neow)
            DepartureArmoryFlow.QueueAfterNeow(RunManager.Instance);
    }
}

public sealed class DepartureArmoryRoomRestorePatch : IPatchMethod
{
    public static string PatchId => "more_weapons_armory_restore";
    public static string Description => "Resume the armory from an already completed Neow save";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() => [new(typeof(EventRoom), nameof(EventRoom.EnterInternal))];
    public static void Prefix(EventRoom __instance)
    {
        if (__instance.CanonicalEvent is Neow)
            DepartureArmoryFlow.SetEnteringNeow(RunManager.Instance, true);
    }
    public static void Postfix(EventRoom __instance, ref Task __result)
    {
        if (__instance.CanonicalEvent is Neow)
            __result = AfterEntry(__result);
    }
    private static async Task AfterEntry(Task original)
    {
        try { await original; }
        finally { DepartureArmoryFlow.SetEnteringNeow(RunManager.Instance, false); }
        DepartureArmoryFlow.QueueAfterNeow(RunManager.Instance);
    }
}

public sealed class DepartureArmoryProceedPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_armory_before_map";
    public static string Description => "Keep the map closed while waiting for the departure armory";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() => [new(typeof(NEventRoom), nameof(NEventRoom.Proceed))];
    public static bool Prefix(ref Task __result)
    {
        if (!DepartureArmoryFlow.IsPendingNeow(RunManager.Instance))
            return true;
        DepartureArmoryFlow.QueueAfterNeow(RunManager.Instance);
        __result = Task.CompletedTask;
        return false;
    }
}
