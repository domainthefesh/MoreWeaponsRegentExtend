using Godot;
using System;
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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public class SovereignScythe : MoreWeaponsCardBase
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const decimal _baseDamage = 6m;
    private const decimal _baseDoom = 10m;

    private decimal _currentDamage = 6m;
    private decimal _totalForged;
    private bool _createdThroughForge;
    private NWeaponVfx? _vfx;

    public override HashSet<CardKeyword> CanonicalKeywords => new() { CardKeyword.Retain };
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DoomPower>()];

    public override TargetType TargetType
    {
        get { if (!HasSeekingEdge) return TargetType.AnyEnemy; return TargetType.AllEnemies; }
    }
    public override bool GainsBlock => GetOwnerParryAmount(this) > 0m;
    private bool HasSeekingEdge => IsMutable && Owner != null && Owner.Creature.HasPower<SeekingEdgePower>();

    private decimal CurrentDamage { get => _currentDamage; set { AssertMutable(); _currentDamage = value; } }
    public bool CreatedThroughForge { get => _createdThroughForge; set { AssertMutable(); _createdThroughForge = value; } }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(_baseDamage, ValueProp.Move),
        new PowerVar<DoomPower>(_baseDoom),
        new CalculationBaseVar(0m), new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => GetOwnerParryAmount(card)),
        new MaxHpVar(4m),
    ];

    public SovereignScythe() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SyncProgressionRewards();
        IEnumerable<Creature> initialTargets = HasSeekingEdge ? CombatState!.HittableEnemies : [cardPlay.Target!];
        var fatalTargets = initialTargets
            .Where(target => target.Powers.All(power => power.ShouldOwnerDeathTriggerFatal()))
            .ToHashSet();
        string animName = "Cast"; float delay = Owner.Character.CastAnimDelay;
        string sfxPath = "event:/sfx/characters/regent/regent_sovereign_blade";

        var cmd = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .WithAttackerAnim(animName, delay).WithAttackerFx(null, sfxPath);

        if (HasSeekingEdge)
            await cmd.TargetingAllOpponents(CombatState!)
                .BeforeDamage(TriggerWeaponAttackFirstEnemy)
                .WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3").Execute(choiceContext);
        else
            await cmd.Targeting(cardPlay.Target!)
                .BeforeDamage(() => TriggerWeaponAttack(cardPlay.Target))
                .WithHitVfxNode(NBigSlashVfx.Create).WithHitVfxNode(NBigSlashImpactVfx.Create).Execute(choiceContext);

        if (GetOwnerParryAmount(this) > 0m)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(cardPlay.Target),
                DynamicVars.CalculatedBlock.Props, cardPlay);

        if (WeaponRewardRuntime.Has(this, 2))
        {
            int kills = cmd.Results.SelectMany(results => results)
                .Where(result => result.WasTargetKilled && fatalTargets.Contains(result.Receiver))
                .Select(result => result.Receiver).Distinct().Count();
            if (kills > 0)
                await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.IntValue * kills);
        }

        decimal doomAmount = DynamicVars["DoomPower"].BaseValue;
        if (doomAmount > 0)
            foreach (var target in GetSurvivingAttackTargets(cmd))
                await PowerCmd.Apply<DoomPower>(choiceContext, target, doomAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3); }

    public void AddDamage(decimal amount)
    {
        AssertMutable();
        DynamicVars.Damage.BaseValue += amount;
        _totalForged += amount;
        CurrentDamage = DynamicVars.Damage.BaseValue - 3m * CurrentUpgradeLevel;
        UpdateDoomAmount();
    }

    private void UpdateDoomAmount() => DynamicVars["DoomPower"].BaseValue =
        (_baseDoom + _totalForged) * (WeaponRewardRuntime.Has(this, 1) ? 1.5m : 1m);
    public void SyncProgressionRewards() { if (IsMutable) UpdateDoomAmount(); }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue = CurrentDamage;
        UpdateDoomAmount();
    }
    protected override void AfterCloned() { base.AfterCloned(); CreatedThroughForge = false; }

    public override void AfterTransformedFrom()
    {
        RemoveWeaponVfx();
        _vfx = null;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != this || IsDupe) return Task.CompletedTask;
        SyncProgressionRewards();
        if (Pile?.IsCombatPile != true || Pile.Type == PileType.Exhaust)
        {
            RemoveWeaponVfx();
            _vfx = null;
        }
        else
            _vfx = EnsureWeaponVfx("orbit/SovereignScythe.tscn");
        return Task.CompletedTask;
    }
}
