using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

/// <summary>Retains Fatal eligibility until the attack settles, including the room's final kill.</summary>
internal static class BladeFatalGrowth
{
    private sealed class Snapshot(Player owner, HashSet<Creature> eligible)
    {
        internal readonly Player Owner = owner;
        internal readonly HashSet<Creature> Eligible = eligible;
        internal bool Completed;
    }

    private static readonly ConditionalWeakTable<AttackCommand, Snapshot> Attacks = new();

    internal static void Capture(AttackCommand command)
    {
        if (command.ModelSource is not SovereignBlade blade || !WeaponRewardRuntime.Has(blade, 2) ||
            blade.CombatState is not { } combat || Attacks.TryGetValue(command, out _))
            return;
        Attacks.Add(command, new Snapshot(blade.Owner, combat.HittableEnemies
            .Where(target => target.Powers.All(power => power.ShouldOwnerDeathTriggerFatal())).ToHashSet()));
    }

    internal static void Complete(AttackCommand command)
    {
        if (!Attacks.TryGetValue(command, out var snapshot) || snapshot.Completed)
            return;
        // Keep the consumed snapshot under its weak command key, so repeated dispatch,
        // multi-hit results and duplicated AfterAttack calls cannot award growth twice.
        snapshot.Completed = true;
        var kills = command.Results.SelectMany(hit => hit)
            .Where(result => result.WasTargetKilled && snapshot.Eligible.Contains(result.Receiver))
            .Select(result => result.Receiver).Distinct().Count();
        if (kills > 0)
            WeaponProgression.AddBladeBaseBonus(snapshot.Owner, 10m * kills);
    }
}
