using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MoreWeaponsRegentExtend.Scripts.Powers;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public sealed class RoyalBrassKnuckles : ForgeableWeaponCardBase
{
    private bool _generationInitialized;

    public override int MaxUpgradeLevel => 0;

    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, MyKeywords.MartialFanatic];

    protected override IEnumerable<DynamicVar> CanonicalVars => AttackVars(10m);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<MartialFanaticPower>(),
        HoverTipFactory.FromPower<AdaptationPower>()
    ];

    public RoyalBrassKnuckles() : base(1, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await AttackWithWeaponVfx(choiceContext, cardPlay);

    public override Task AfterCardEnteredCombat(CardModel card) => card == this
        ? InitializeForCombat()
        : Task.CompletedTask;

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        await base.AfterCardChangedPiles(card, oldPileType, clonedBy);
        if (card == this)
            await InitializeForCombat();
    }

    private async Task InitializeForCombat()
    {
        if (_generationInitialized || IsDupe || Owner?.PlayerCombatState == null
            || Pile?.IsCombatPile != true || Pile.Type == PileType.Exhaust || CombatState == null)
            return;

        // Mark before exhaust hooks run: those hooks may move or clone this weapon.
        _generationInitialized = true;
        if (LocalContext.NetId is { } localPlayerId)
        {
            // Exhaust triggers may themselves request a card choice. Use the same
            // continuation handling as the game's hooks that have no supplied context.
            var context = new HookPlayerChoiceContext(this, localPlayerId, CombatState, GameActionType.Combat);
            await context.AssignTaskAndWaitForPauseOrCompletion(ActivateMartialFanatic(context));
        }
        else
        {
            await ActivateMartialFanatic(new BlockingPlayerChoiceContext());
        }
    }

    private async Task ActivateMartialFanatic(PlayerChoiceContext choiceContext)
    {
        if (!Owner.Creature.HasPower<MartialFanaticPower>())
            await PowerCmd.Apply<MartialFanaticPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);

        // PlayerCombatState excludes the permanent Deck. Snapshot before moving cards
        // and exhaust through the normal command so exhaust reactions still work.
        var basicAttacks = Owner.PlayerCombatState!.AllCards
            .Where(card => !card.IsDupe && card.Type == CardType.Attack && card.Rarity == CardRarity.Basic
                && card.Pile?.IsCombatPile == true && card.Pile.Type != PileType.Exhaust)
            .ToArray();
        foreach (var card in basicAttacks)
        {
            if (card.Pile?.IsCombatPile == true && card.Pile.Type != PileType.Exhaust)
                await CardCmd.Exhaust(choiceContext, card);
        }
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _generationInitialized = false;
    }
}
