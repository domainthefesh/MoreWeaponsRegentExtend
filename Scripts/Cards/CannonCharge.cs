using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public sealed class CannonCharge : MoreWeaponsCardBase
{
    private CannonChargeLink? _link;
    private int _slot = -1;

    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedByModifiers => false;
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("ChargeSlot", 1m), new DynamicVar("CannonNumber", 0m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        IsMutable && _link != null ? [HoverTipFactory.FromCard(_link.Cannon)] : [];

    public CannonCharge() : base(1, CardType.Skill, CardRarity.Token, TargetType.Self, true)
    {
    }

    public void Bind(CannonChargeLink link, int slot)
    {
        AssertMutable();
        _link = link;
        _slot = slot;
        DynamicVars["ChargeSlot"].BaseValue = slot + 1;
        DynamicVars["CannonNumber"].BaseValue = link.Number;
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Only successfully playing the charge counts: discarding, exhausting,
        // or removing it never completes a slot. Replays/copies share that slot.
        var owner = Owner;
        if (_link is { } link && owner != null && link.Cannon.Owner == owner
            && !link.Cannon.HasBeenRemovedFromState)
            link.CompleteSlot(_slot);
        NUpdateCardVfx.Play(this, owner?.Creature);
        return Task.CompletedTask;
    }
}
