using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Patching.Models;

namespace MoreWeaponsRegentExtend.Scripts;

/// <summary>Protect the actual forge body when a caller has inlined its async wrapper.</summary>
public sealed class ForgeAsyncEntryPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_forge_async_entry";
    public static string Description => "Route inlined Forge calls through the same weapon selection logic";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.AsyncMethod(typeof(ForgeCmd), nameof(ForgeCmd.Forge))];

    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
    {
        var body = instructions.ToList();
        var stateMachine = typeof(ForgeCmd).GetMethod(nameof(ForgeCmd.Forge))!
            .GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        if (__originalMethod.DeclaringType != stateMachine || body.Count == 0)
            throw new InvalidOperationException("Forge async entry could not be resolved.");

        // Resolve and check compiler fields once while installing the patch. Runtime
        // calls use normal IL field access, without reflection or scanning callers.
        var state = RequiredField(stateMachine, "<>1__state", typeof(int));
        var builderType = typeof(AsyncTaskMethodBuilder<IEnumerable<SovereignBlade>>);
        var builder = RequiredField(stateMachine, "<>t__builder", builderType);
        var amount = RequiredField(stateMachine, "amount", typeof(decimal));
        var player = RequiredField(stateMachine, "player", typeof(Player));
        var source = RequiredField(stateMachine, "source", typeof(AbstractModel));
        var taskType = typeof(Task<IEnumerable<SovereignBlade>>);
        var redirectedTask = generator.DeclareLocal(taskType);
        var originalEntry = generator.DefineLabel();
        body[0].labels.Add(originalEntry);

        var guard = typeof(ForgeAsyncEntryPatch).GetMethod(nameof(TryRedirect))!;
        var complete = typeof(ForgeAsyncEntryPatch).GetMethod(nameof(CompleteRedirectedForge))!;
        var getTask = builderType.GetProperty(nameof(AsyncTaskMethodBuilder.Task))!.GetMethod!;

        var entry = new List<CodeInstruction>
        {
            // Only the first invocation may route the operation. Resumed native
            // awaits keep the original state machine and never forge twice.
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, state),
            new(OpCodes.Ldc_I4_M1),
            new(OpCodes.Bne_Un, originalEntry),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, amount),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, player),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, source),
            new(OpCodes.Ldloca, redirectedTask),
            new(OpCodes.Call, guard),
            new(OpCodes.Brfalse, originalEntry),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldc_I4, -2),
            new(OpCodes.Stfld, state),
            // Materialize the promise BEFORE copying the value-type builder so
            // the bridge completes the same Task that the original wrapper returns.
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldflda, builder),
            new(OpCodes.Call, getTask),
            new(OpCodes.Pop),
            new(OpCodes.Ldloc, redirectedTask),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, builder),
            new(OpCodes.Call, complete),
            new(OpCodes.Pop),
            new(OpCodes.Ret)
        };
        entry.AddRange(body);
        return entry;
    }

    private static FieldInfo RequiredField(Type stateMachine, string name, Type fieldType)
    {
        var field = stateMachine.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field?.FieldType != fieldType)
            throw new InvalidOperationException($"Forge async field '{name}' changed; cannot safely install weapon routing.");
        return field;
    }

    public static bool TryRedirect(
        decimal amount, Player player, AbstractModel? source,
        out Task<IEnumerable<SovereignBlade>> task)
    {
        try
        {
            return ForgeRandomizePatch.TryHandleForge(amount, player, source, out task);
        }
        catch (Exception exception)
        {
            // The injected entry precedes the native try block. Preserve async
            // exception delivery instead of leaving the caller's promise unfinished.
            task = Task.FromException<IEnumerable<SovereignBlade>>(exception);
            return true;
        }
    }

    public static async Task CompleteRedirectedForge(
        Task<IEnumerable<SovereignBlade>> task,
        AsyncTaskMethodBuilder<IEnumerable<SovereignBlade>> builder)
    {
        try
        {
            builder.SetResult(await task);
        }
        catch (Exception exception)
        {
            builder.SetException(exception);
        }
    }
}
