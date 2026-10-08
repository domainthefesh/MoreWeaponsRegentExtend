using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MoreWeaponsRegentExtend.Scripts.Progression;
using MoreWeaponsRegentExtend.Scripts.Powers;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public sealed class NeptuneTrident : ForgeableWeaponCardBase
{
    private int _returnAtTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars => AttackVars(9m, new PowerVar<LightningRodPower>(1m));

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<LightningRodPower>();
            if (WeaponRewardRuntime.Has(this, 1))
                yield return HoverTipFactory.FromPower<ThunderPower>();
            if (WeaponRewardRuntime.Has(this, 2))
                yield return HoverTipFactory.FromPower<TsunamiPower>();
        }
    }

    public void SyncProgressionRewards() => DynamicVars["LightningRodPower"].BaseValue = 1m;

    public NeptuneTrident() : base(2, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SyncProgressionRewards();
        // Replays are temporary dupes; schedule the actual card instead of returning a dupe.
        var actualCard = IsDupe ? DupeOf as NeptuneTrident : this;
        if (actualCard != null)
            actualCard._returnAtTurn = Owner.PlayerCombatState!.TurnNumber + 1;

        if (WeaponRewardRuntime.Has(this, 1) && WeaponRewardRuntime.IsFirstPlay(this, cardPlay))
            await PowerCmd.Apply<ThunderPower>(choiceContext, Owner.Creature, 10m, Owner.Creature, this);

        if (WeaponRewardRuntime.Has(this, 2))
            foreach (var target in GetAttackTargets(cardPlay))
                if (target.IsAlive && !target.HasPower<TsunamiPower>())
                    await PowerCmd.Apply<TsunamiPower>(choiceContext, target, 1m, Owner.Creature, this);
        await AttackWithWeaponVfx(choiceContext, cardPlay);
        await PowerCmd.Apply<LightningRodPower>(choiceContext, Owner.Creature,
            DynamicVars["LightningRodPower"].BaseValue, Owner.Creature, this);
    }

    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState) =>
        player == Owner ? TryReturnToHand() : Task.CompletedTask;

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        cardPlay.Card.Owner == Owner ? TryReturnToHand() : Task.CompletedTask;

    private async Task TryReturnToHand()
    {
        if (IsDupe || _returnAtTurn == 0 || HasBeenRemovedFromState
            || Owner.PlayerCombatState == null || Owner.PlayerCombatState.TurnNumber < _returnAtTurn
            || CombatManager.Instance.IsOverOrEnding)
            return;

        if (Pile?.Type == PileType.Hand)
        {
            _returnAtTurn = 0;
            return;
        }
        if (Pile?.IsCombatPile != true || PileType.Hand.GetPile(Owner).Cards.Count >= CardPile.MaxCardsInHand)
            return;

        await CardPileCmd.Add(this, PileType.Hand);
        // Preserve the pending return if another effect redirects the move or the hand fills.
        if (Pile?.Type == PileType.Hand)
            _returnAtTurn = 0;
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _returnAtTurn = 0;
    }
}
