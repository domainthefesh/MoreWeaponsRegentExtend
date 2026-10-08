using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Localization;
using MoreWeaponsRegentExtend.Scripts.Progression;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class AdaptationPower : ModPowerTemplate
{
    public const int MaximumStacks = 2;

    public static int GetMaximum(Creature? creature) => MaximumStacks +
        (creature?.Player is { } player && WeaponProgression.HasReward(player, WeaponKind.RoyalBrassKnuckles, 2) ? 2 : 0);

    public void SyncProgressionRewards() => ClampAmount();

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add("MaximumStacks", GetMaximum(IsMutable ? Owner : null));
            return description;
        }
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/AdaptationPower.png",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/AdaptationPower.png"
    );

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        decimal adjustment = Math.Clamp(Amount, 0, GetMaximum(Owner)) * 0.2m;
        decimal multiplier = 1m;
        // Includes Unpowered damage. Direct Kill / SetCurrentHp effects bypass damage hooks.
        if (target == Owner)
            multiplier *= Math.Max(0m, 1m - adjustment);
        if (dealer == Owner)
            multiplier *= 1m + adjustment;
        return multiplier;
    }

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target,
        decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not AdaptationPower || target != Owner || amount <= 0m)
            return false;

        modifiedAmount = Math.Min(amount, Math.Max(0, GetMaximum(Owner) - Amount));
        return modifiedAmount != amount;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ClampAmount();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
            ClampAmount();
        return Task.CompletedTask;
    }

    private void ClampAmount()
    {
        var maximum = GetMaximum(Owner);
        if (Amount > maximum)
            SetAmount(maximum);
    }
}
