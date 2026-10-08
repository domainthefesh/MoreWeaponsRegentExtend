using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public sealed class GalaxyTrajectoryCannon : ForgeableWeaponCardBase
{
    private CannonChargeLink? _chargeLink;

    public CannonChargeLink ChargeLink
    {
        get
        {
            AssertMutable();
            return IsDupe && DupeOf is GalaxyTrajectoryCannon original
                ? original.ChargeLink
                : _chargeLink ??= new CannonChargeLink(this);
        }
    }

    public bool IsHot => IsMutable && ChargeLink.IsHot;

    public override int MaxUpgradeLevel => 0;
    public override TargetType TargetType => TargetType.AllEnemies;
    protected override decimal ForgeDamageMultiplier => 10m;
    protected override bool IsPlayable => IsHot;
    protected override bool ShouldGlowGoldInternal => IsHot;

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust, MyKeywords.HeatDeath];

    protected override IEnumerable<DynamicVar> CanonicalVars => AttackVars(50m,
        new DynamicVar("ChargesPlayed", 0m),
        new DynamicVar("ChargesRequired", CannonChargeLink.RequiredCharges),
        new DynamicVar("HeatStarted", 0m), new DynamicVar("CannonNumber", 0m));

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<CannonCharge>();

    public GalaxyTrajectoryCannon() : base(3, CardType.Attack, TargetType.AllEnemies)
    {
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        // A replay's temporary card is also a combat hook listener. Only the
        // card being played contributes; its source must not add a second replay.
        HasSeekingEdge && card == this
            ? playCount + 1 : playCount;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // The wrapper/AutoPlay patches reject a cold cannon before side effects.
        // Keep the effect itself guarded as well for direct invocations by other mods.
        if (!IsHot)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithAttackerAnim("Cast", Owner.Character.CastAnimDelay)
            .WithAttackerFx(null, "event:/sfx/characters/regent/regent_sovereign_blade")
            .BeforeDamage(TriggerWeaponAttackFirstEnemy)
            .Execute(choiceContext);

        if (GetOwnerParryAmount(this) > 0m)
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars.CalculatedBlock.Calculate(cardPlay.Target),
                DynamicVars.CalculatedBlock.Props, cardPlay);
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        await base.AfterCardChangedPiles(card, oldPileType, clonedBy);
        if (card == this)
        {
            RefreshThermalState();
            await CreateBoundCharges();
        }
    }

    public override Task AfterCardEnteredCombat(CardModel card) =>
        card == this ? CreateBoundCharges() : Task.CompletedTask;

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator) =>
        card == this ? CreateBoundCharges() : Task.CompletedTask;

    private async Task CreateBoundCharges()
    {
        if (!IsMutable || IsDupe || Owner == null || CombatState == null
            || Pile?.IsCombatPile != true || HasBeenRemovedFromState || ChargeLink.ChargesCreated)
            return;

        ChargeLink.ChargesCreated = true;
        var charges = new List<CardModel>(ChargeLink.RequiredCount);
        for (int slot = 0; slot < ChargeLink.RequiredCount; slot++)
        {
            var charge = CombatState.CreateCard<CannonCharge>(Owner);
            charge.Bind(ChargeLink, slot);
            charges.Add(charge);
        }

        // Full hands use the game's normal overflow-to-discard behavior. The cards
        // retain their slots there and can still charge this cannon when drawn.
        var results = await CardPileCmd.AddGeneratedCardsToCombat(charges, PileType.Hand, Owner);
        CardCmd.PreviewCardPileAdd(results);
    }

    public void RefreshThermalState()
    {
        if (!IsMutable)
            return;
        DynamicVars["ChargesRequired"].BaseValue = ChargeLink.RequiredCount;
        DynamicVars["ChargesPlayed"].BaseValue = System.Math.Min(ChargeLink.CompletedCharges, ChargeLink.RequiredCount);
        DynamicVars["HeatStarted"].BaseValue = IsHot ? 1m : 0m;
        DynamicVars["CannonNumber"].BaseValue = ChargeLink.Number;
        if (IsHot)
        {
            if (Keywords.Contains(MyKeywords.HeatDeath))
                RemoveKeyword(MyKeywords.HeatDeath);
            if (!Keywords.Contains(MyKeywords.HotStart))
                AddKeyword(MyKeywords.HotStart);
        }
        else
        {
            if (Keywords.Contains(MyKeywords.HotStart))
                RemoveKeyword(MyKeywords.HotStart);
            if (!Keywords.Contains(MyKeywords.HeatDeath))
                AddKeyword(MyKeywords.HeatDeath);
        }
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _chargeLink = new CannonChargeLink(this);
        RefreshThermalState();
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        RefreshThermalState();
    }
}
