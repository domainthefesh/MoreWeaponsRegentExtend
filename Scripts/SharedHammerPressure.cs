using System;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using MoreWeaponsRegentExtend.Scripts.Cards;

namespace MoreWeaponsRegentExtend.Scripts;

// One threshold per player/combat: replacing a hammer must not display a lower
// application threshold than the hammer it replaced. Attack damage remains per card.
public static class SharedHammerPressure
{
    private sealed class ThresholdState
    {
        public decimal Maximum = 10m;
    }

    private static readonly ConditionalWeakTable<PlayerCombatState, ThresholdState> States = new();

    public static decimal Observe(SovereignBludgeon hammer)
    {
        if (!hammer.IsMutable || hammer.Owner?.PlayerCombatState is not { } combat)
            return hammer.LocalPressureThreshold;

        var state = States.GetValue(combat, static _ => new ThresholdState());
        var hammers = combat.AllCards.OfType<SovereignBludgeon>().Where(card => !card.IsDupe).ToArray();
        if (!hammer.IsDupe)
            state.Maximum = Math.Max(state.Maximum, hammer.LocalPressureThreshold);
        foreach (var card in hammers)
            state.Maximum = Math.Max(state.Maximum, card.LocalPressureThreshold);

        foreach (var card in hammers)
            card.SyncPressureThreshold(state.Maximum);
        hammer.SyncPressureThreshold(state.Maximum);
        return state.Maximum;
    }
}
