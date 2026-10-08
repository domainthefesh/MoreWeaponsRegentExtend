using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MoreWeaponsRegentExtend.Scripts.Progression;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class HighGroundPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/HighGroundPower.png",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/HighGroundPower.png"
    );

    private decimal DamageAdjustment => IsMutable && Owner?.Player is { } player &&
        WeaponProgression.HasReward(player, WeaponKind.SovereignWings, 2) ? 75m : 50m;

    public void SyncProgressionRewards()
    {
        DynamicVars["DamageDecrease"].BaseValue = DamageAdjustment;
        DynamicVars["DamageIncrease"].BaseValue = DamageAdjustment;
    }

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add("DamageDecrease", DamageAdjustment);
            description.Add("DamageIncrease", DamageAdjustment);
            return description;
        }
    }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DamageDecrease", 50m),
        new DynamicVar("DamageIncrease", 50m)
    ];

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // This includes Unpowered damage as specified; Kill/SetCurrentHp bypass damage hooks.
        decimal multiplier = 1m;
        if (target == Owner)
            multiplier *= 1m - DamageAdjustment / 100m;
        if (dealer == Owner)
            multiplier *= 1m + DamageAdjustment / 100m;
        return multiplier;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? source, CardModel? card)
    {
        if (target != Owner || result.UnblockedDamage <= 0)
            return;
        Flash();
        await PowerCmd.Decrement(this);
    }
}
