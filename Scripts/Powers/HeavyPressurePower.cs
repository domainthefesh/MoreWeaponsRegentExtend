using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MoreWeaponsRegentExtend.Scripts.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class HeavyPressurePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/HeavyPressurePower.png",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/HeavyPressurePower.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamageThreshold", 10m)];

    public decimal DamageThreshold => DynamicVars["DamageThreshold"].BaseValue;

    // Capture the weapon's forge value at application; subsequent forging does not change this debuff.
    public void RaiseThreshold(decimal threshold)
    {
        AssertMutable();
        DynamicVars["DamageThreshold"].BaseValue = System.Math.Max(DamageThreshold, threshold);
    }

    public decimal FilterDamage(decimal finalDamage) => finalDamage < DamageThreshold ? 0m : finalDamage;

    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount > 0 && cardSource is SovereignBludgeon hammer)
            RaiseThreshold(hammer.PressureThreshold);
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        // Covers stacking as well as first application, before PowerCmd refreshes enemy intents.
        if (power == this && amount > 0 && cardSource is SovereignBludgeon hammer)
            RaiseThreshold(hammer.PressureThreshold);
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
            await PowerCmd.TickDownDuration(this);
    }
}
