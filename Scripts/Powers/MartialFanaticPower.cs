using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MoreWeaponsRegentExtend.Scripts.Cards;
using MoreWeaponsRegentExtend.Scripts.Progression;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class MartialFanaticPower : ModPowerTemplate
{
    public decimal ForgeAmount => IsMutable && Owner?.Player is { } player &&
        WeaponProgression.HasReward(player, WeaponKind.RoyalBrassKnuckles, 1) ? 10m : 8m;

    public void SyncProgressionRewards() { }

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add("ForgeAmount", ForgeAmount);
            description.Add("MaximumStacks", AdaptationPower.GetMaximum(IsMutable ? Owner : null));
            return description;
        }
    }
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/MartialFanaticPower.png",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/MartialFanaticPower.png"
    );

    // The game consults this hook for both manual plays and CardCmd.AutoPlay.
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        card.Owner?.Creature != Owner || card.Type != CardType.Attack || card is RoyalBrassKnuckles;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || result.UnblockedDamage <= 0 || Owner.Player?.PlayerCombatState == null)
            return;

        // One forge per real damage result, including each hit of a multi-hit move.
        // Forge changes weapon values, but causes no damage itself and cannot recurse.
        Flash();
        await ForgeCmd.Forge(ForgeAmount, Owner.Player, this);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer == null || dealer.Side == Owner.Side
            || !result.WasFullyBlocked || result.BlockedDamage <= 0 || result.UnblockedDamage != 0)
            return;
        if (Owner.GetPowerAmount<AdaptationPower>() >= AdaptationPower.GetMaximum(Owner))
            return;

        // BlockedDamage > 0 distinguishes a real block from a zero-damage filter.
        Flash();
        await PowerCmd.Apply<AdaptationPower>(choiceContext, Owner, 1m, Owner, cardSource);
    }
}
