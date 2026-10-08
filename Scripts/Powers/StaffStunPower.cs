using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class StaffStunPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/StaffStunPower.png",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/StaffStunPower.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [StunIntent.GetStaticHoverTip()];

    public static async Task ApplyForTwoTurns(PlayerChoiceContext choiceContext, Creature target,
        Creature applier, CardModel cardSource)
    {
        var existing = target.GetPower<StaffStunPower>();
        if (existing == null)
            await PowerCmd.Apply<StaffStunPower>(choiceContext, target, 2m, applier, cardSource);
        else
        {
            // Repeated Heavy Chops renew the duration instead of stacking extra skipped turns.
            if (existing.Amount < 2)
                await PowerCmd.ModifyAmount(choiceContext, existing, 2m - existing.Amount, applier, cardSource);
            await existing.EnsureStunned();
        }
    }

    private Task EnsureStunned() => Amount > 0 && Owner.IsAlive && Owner.Monster != null && !Owner.IsStunned
        ? CreatureCmd.Stun(Owner)
        : Task.CompletedTask;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) => EnsureStunned();

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        // Player-turn setup rolls the monster away from its previous STUNNED move. Replace
        // that newly rolled intent while duration remains, so both enemy actions are skipped.
        return EnsureStunned();
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
            await PowerCmd.TickDownDuration(this);
    }
}
