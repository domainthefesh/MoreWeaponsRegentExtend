using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MoreWeaponsRegentExtend.Scripts.Powers;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public enum StaffCombatStance
{
    LightSwing,
    Sweep,
    HeavyChop
}

public sealed class SeaCalmingStaff : ForgeableWeaponCardBase
{
    private const decimal BaseDamage = 8m;
    private StaffCombatStance _stance;
    private int _stanceSwitches;

    public override int MaxUpgradeLevel => 0;
    public StaffCombatStance Stance => _stance;
    public decimal StanceDamageMultiplier => _stance switch
    {
        StaffCombatStance.LightSwing => 0.8m,
        StaffCombatStance.HeavyChop => 1.2m,
        _ => 1m
    };
    public int StanceEnergyCost => _stance switch
    {
        StaffCombatStance.LightSwing => 1,
        StaffCombatStance.HeavyChop => 3,
        _ => 2
    };
    public int StanceHitCount => _stance == StaffCombatStance.Sweep && HasSeekingEdge ? 2 : 1;

    // The weapon's normal cost is 2; its initial Light Swing reduces it to 1.
    protected override int CanonicalEnergyCost => 1;
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, MyKeywords.Stance];
    public override TargetType TargetType => _stance == StaffCombatStance.Sweep || HasSeekingEdge
        ? TargetType.AllEnemies
        : TargetType.AnyEnemy;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<StaffStunPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => AttackVars(BaseDamage * 0.8m,
        new DynamicVar("Stance", 0m), new DynamicVar("StunTurns", 2m), new DynamicVar("Hits", 1m));

    public SeaCalmingStaff() : base(2, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    public void AdvanceStance()
    {
        AssertMutable();
        _stance = (StaffCombatStance)(((int)_stance + 1) % 3);
        _stanceSwitches++;
        RefreshStance();
    }

    private void RefreshStance()
    {
        DynamicVars.Damage.BaseValue = (BaseDamage + ForgedAmount) * StanceDamageMultiplier;
        DynamicVars["Stance"].BaseValue = (int)_stance;
        DynamicVars["Hits"].BaseValue = StanceHitCount;
        EnergyCost.SetCustomBaseCost(StanceEnergyCost);
    }

    protected override void OnForgeAmountChanged() => RefreshStance();

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _stanceSwitches = 0;
        RefreshStance();
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card == this)
            RefreshStance();
        await base.AfterCardChangedPiles(card, oldPileType, clonedBy);
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (IsMutable && Owner != null && power.Owner == Owner.Creature)
            DynamicVars["Hits"].BaseValue = StanceHitCount;
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Dupe replays belong to the source weapon. Real card copies retain independent stances.
        var weapon = IsDupe && DupeOf is SeaCalmingStaff source ? source : this;
        if (weapon != this)
        {
            _stance = weapon._stance;
            RefreshStance();
            DynamicVars.Damage.BaseValue = weapon.DynamicVars.Damage.BaseValue;
        }
        var playedStance = _stance;
        bool hitsAll = playedStance == StaffCombatStance.Sweep || HasSeekingEdge;
        var firstTarget = hitsAll
            ? CombatState!.HittableEnemies.FirstOrDefault()
            : cardPlay.Target ?? CombatState!.HittableEnemies.FirstOrDefault();
        if (firstTarget != null)
        {
            var command = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .WithHitCount(StanceHitCount)
                .WithAttackerAnim("Cast", Owner.Character.CastAnimDelay)
                .WithAttackerFx(null, "event:/sfx/characters/regent/regent_sovereign_blade")
                .BeforeDamage(() => hitsAll ? TriggerWeaponAttackFirstEnemy() : TriggerWeaponAttack(firstTarget));
            if (hitsAll)
                command.TargetingAllOpponents(CombatState!);
            else
                command.Targeting(firstTarget);
            var attack = await command.Execute(choiceContext);

            if (playedStance == StaffCombatStance.HeavyChop)
            {
                foreach (var target in attack.Results.SelectMany(results => results)
                             .Where(result => result.UnblockedDamage > 0)
                             .Select(result => result.Receiver).Distinct()
                             .Where(target => target.IsAlive && target.Monster != null))
                    await StaffStunPower.ApplyForTwoTurns(choiceContext, target, Owner.Creature, this);
            }
            if (GetOwnerParryAmount(this) > 0m)
                await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(cardPlay.Target),
                    DynamicVars.CalculatedBlock.Props, cardPlay);
        }
        weapon.AdvanceStance();
        if (WeaponRewardRuntime.Has(weapon, 2) && weapon._stanceSwitches % 3 == 0)
            await PlayerCmd.GainEnergy(3m, Owner);
        if (weapon != this)
        {
            _stance = weapon._stance;
            RefreshStance();
        }
    }
}
