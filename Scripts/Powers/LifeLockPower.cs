using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class LifeLockPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/LifeLockPower.svg",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/LifeLockPower.svg");

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target,
        decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (target != Owner || amount <= 0)
            return false;
        // Fate Lock itself neither amplifies nor stacks. Other debuffs retain the
        // game's artifact/immunity checks and normal duration/amount semantics.
        modifiedAmount = canonicalPower is LifeLockPower ? 0 :
            canonicalPower.Type == PowerType.Debuff ? amount + 2m : amount;
        return modifiedAmount != amount;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Amount != 1)
            SetAmount(1);
        return Task.CompletedTask;
    }
}
