using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class ResentmentCursePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    // Reuse the mod's existing chain/lock icon for the combat curse.
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/LifeLockPower.svg",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/LifeLockPower.svg");

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
        => target == Owner ? 2m : 1m;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Amount != 1)
            SetAmount(1);
        return Task.CompletedTask;
    }

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target,
        decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = canonicalPower is ResentmentCursePower && target == Owner && amount > 0 ? 0m : amount;
        return modifiedAmount != amount;
    }
}
