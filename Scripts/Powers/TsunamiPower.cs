using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class TsunamiPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/TsunamiPower.svg",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/TsunamiPower.svg");

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Amount != 1)
            SetAmount(1);
        // Existing Block is reduced before the trident's attack. Future Block
        // gains are halved through the native modifier, not reduced twice.
        await CreatureCmd.LoseBlock(new ThrowingPlayerChoiceContext(), Owner,
            Owner.Block - decimal.Floor(Owner.Block * 0.5m), applier);
    }

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block,
        ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
        => target == Owner ? 0.5m : 1m;

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target,
        decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = canonicalPower is TsunamiPower && target == Owner && amount > 0 ? 0 : amount;
        return modifiedAmount != amount;
    }
}
