using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Hooks;
using STS2RitsuLib.Patching.Models;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

/// <summary>The native hook dispatcher skips combat models as soon as the final enemy dies.</summary>
public sealed class BladeFatalAfterAttackPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_blade_fatal_after_attack";
    public static string Description => "Count native blade Fatal growth before the ending-combat hook gate";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.AsyncMethod(typeof(Hook), nameof(Hook.AfterAttack))];

    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
    {
        var body = instructions.ToList();
        var machine = typeof(Hook).GetMethod(nameof(Hook.AfterAttack))!
            .GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var state = machine.GetField("<>1__state", flags);
        var command = machine.GetField("command", flags);
        if (__originalMethod.DeclaringType != machine || body.Count == 0 ||
            state?.FieldType != typeof(int) || command?.FieldType != typeof(AttackCommand))
            throw new InvalidOperationException("AfterAttack async fields changed; cannot safely count native blade Fatal.");

        // Patch the actual async body, which remains reachable even if its small
        // wrapper was inlined before mods loaded. Resumed awaits never count again.
        var originalEntry = generator.DefineLabel();
        body[0].labels.Add(originalEntry);
        var complete = typeof(BladeFatalGrowth).GetMethod(nameof(BladeFatalGrowth.Complete),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var prefix = new List<CodeInstruction>
        {
            new(OpCodes.Ldarg_0), new(OpCodes.Ldfld, state), new(OpCodes.Ldc_I4_M1),
            new(OpCodes.Bne_Un, originalEntry),
            new(OpCodes.Ldarg_0), new(OpCodes.Ldfld, command), new(OpCodes.Call, complete)
        };
        prefix.AddRange(body);
        return prefix;
    }
}
