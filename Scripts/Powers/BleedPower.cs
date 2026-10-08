using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MoreWeaponsRegentExtend.Scripts.Cards;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class BleedPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BleedPercent", 3m)];

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add("BleedPercent", DynamicVars["BleedPercent"].BaseValue);
            return description;
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ObserveBleedSource(cardSource);
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this && amount > 0m) ObserveBleedSource(cardSource);
        return Task.CompletedTask;
    }

    private void ObserveBleedSource(CardModel? cardSource)
    {
        if (cardSource is SovereignAxe axe)
            DynamicVars["BleedPercent"].BaseValue = Math.Max(
                DynamicVars["BleedPercent"].BaseValue, axe.BleedPercent);
    }

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/BleedPower.png",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/BleedPower.png"
    );

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? source,
        CardModel? card)
    {
        if (target != base.Owner || !props.IsPoweredAttack() || result.TotalDamage <= 0)
            return;

        decimal bleedDamage = base.Owner.MaxHp * DynamicVars["BleedPercent"].BaseValue / 100m;
        if (bleedDamage < 1m)
            bleedDamage = 1m;

        await CreatureCmd.Damage(choiceContext, base.Owner, bleedDamage,
            ValueProp.Unblockable | ValueProp.Unpowered, base.Owner);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
            await PowerCmd.Decrement(this);
    }
}
