using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class RendPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/RendPower.svg",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/RendPower.svg");

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != Owner || !Owner.IsAlive || Amount <= 0)
            return;
        var creature = Owner;
        var loss = decimal.Ceiling(creature.MaxHp * 0.1m);
        Flash();
        await PowerCmd.Decrement(this);
        // One activation per AttackCommand, not one per hit or victim. Stacks
        // represent the remaining attacks, never multiply the 10% HP loss.
        await CreatureCmd.Damage(choiceContext, creature, loss,
            ValueProp.Unblockable | ValueProp.Unpowered, creature);
    }
}
