using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

// New weapons receive each forge event once through ForgeSingleton.
public abstract class ForgeableWeaponCardBase : MoreWeaponsCardBase
{
    public decimal ForgedAmount { get; private set; }

    protected virtual decimal ForgeDamageMultiplier => 1m;

    protected bool HasSeekingEdge => IsMutable && Owner != null && Owner.Creature.HasPower<SeekingEdgePower>();

    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    public override TargetType TargetType => Type == CardType.Attack && HasSeekingEdge
        ? TargetType.AllEnemies
        : base.TargetType;

    public override bool GainsBlock => GetOwnerParryAmount(this) > 0m;

    protected ForgeableWeaponCardBase(int energyCost, CardType type, TargetType targetType)
        : base(energyCost, type, CardRarity.Token, targetType, true)
    {
    }

    public void AddForge(decimal amount)
    {
        AssertMutable();
        ForgedAmount += amount;
        if (DynamicVars.ContainsKey("Damage"))
            DynamicVars.Damage.BaseValue += amount * ForgeDamageMultiplier;
        OnForgeAmountChanged();
        AfterForged();
    }

    protected virtual void OnForgeAmountChanged()
    {
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != this || IsDupe)
            return Task.CompletedTask;

        if (Pile?.IsCombatPile != true || Pile.Type == PileType.Exhaust)
            RemoveWeaponVfx();
        else
            EnsureWeaponVfx($"orbit/{GetType().Name}.tscn");

        return Task.CompletedTask;
    }

    public override void AfterTransformedFrom() => RemoveWeaponVfx();

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        if (DynamicVars.ContainsKey("Damage"))
            DynamicVars.Damage.BaseValue += ForgedAmount * ForgeDamageMultiplier;
        OnForgeAmountChanged();
    }

    protected static IEnumerable<DynamicVar> AttackVars(decimal damage, params DynamicVar[] extraVars) =>
    [
        new DamageVar(damage, ValueProp.Move),
        .. ParryVars(),
        .. extraVars
    ];

    protected IReadOnlyList<Creature> GetAttackTargets(CardPlay cardPlay) => HasSeekingEdge
        ? CombatState!.HittableEnemies.ToArray()
        : [cardPlay.Target!];

    protected async Task<AttackCommand> AttackWithWeaponVfx(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature? vfxTarget = GetAttackTargets(cardPlay).FirstOrDefault();
        var command = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .WithAttackerAnim("Cast", Owner.Character.CastAnimDelay)
            .WithAttackerFx(null, "event:/sfx/characters/regent/regent_sovereign_blade")
            .BeforeDamage(() =>
            {
                NWeaponVfx.AttackFor(this, vfxTarget);
                NUpdateCardVfx.Play(this, vfxTarget);
                return Task.CompletedTask;
            });

        if (HasSeekingEdge)
            command.TargetingAllOpponents(CombatState!);
        else
            command.Targeting(cardPlay.Target!);

        var result = await command.Execute(choiceContext);
        await GainParryBlock(cardPlay);
        return result;
    }
}
