using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.ValueProps;
using MoreWeaponsRegentExtend.Scripts;
using STS2RitsuLib.Keywords;
using MoreWeaponsRegentExtend.Scripts.Powers;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public class ApocalypseLongbow : MoreWeaponsCardBase
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const decimal _baseDamage = 8m;

    private decimal _currentDamage = 8m;
    private decimal _totalForged;
    private bool _createdThroughForge;
    private NWeaponVfx? _vfx;

    public override HashSet<CardKeyword> CanonicalKeywords => new() { CardKeyword.Retain, MyKeywords.Apocalypse };
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => WeaponRewardRuntime.Has(this, 2)
        ? [HoverTipFactory.FromKeyword(MyKeywords.Apocalypse), HoverTipFactory.FromPower<LifeLockPower>()]
        : [HoverTipFactory.FromKeyword(MyKeywords.Apocalypse)];

    public override TargetType TargetType
    {
        get { if (!HasSeekingEdge) return TargetType.AnyEnemy; return TargetType.AllEnemies; }
    }
    public override bool GainsBlock => GetOwnerParryAmount(this) > 0m;
    private bool HasSeekingEdge => IsMutable && Owner != null && Owner.Creature.HasPower<SeekingEdgePower>();

    private decimal CurrentDamage { get => _currentDamage; set { AssertMutable(); _currentDamage = value; } }
    public int DebuffCount => (int)DynamicVars["DebuffCount"].BaseValue;
    public bool CreatedThroughForge { get => _createdThroughForge; set { AssertMutable(); _createdThroughForge = value; } }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(_baseDamage, ValueProp.Move), new DynamicVar("DebuffCount", 1m),
        new CalculationBaseVar(0m), new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => GetOwnerParryAmount(card)),
        new PowerVar<LifeLockPower>(1),
    ];

    public ApocalypseLongbow() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SyncProgressionRewards();
        string animName = "Cast"; float delay = Owner.Character.CastAnimDelay;
        string sfxPath = "event:/sfx/characters/regent/regent_sovereign_blade";
        var attackCmd = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .WithAttackerAnim(animName, delay).WithAttackerFx(null, sfxPath);

        if (HasSeekingEdge)
            await attackCmd.TargetingAllOpponents(CombatState!)
                .BeforeDamage(TriggerWeaponAttackFirstEnemy)
                .WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3").Execute(choiceContext);
        else
            await attackCmd.Targeting(cardPlay.Target!)
                .BeforeDamage(() => TriggerWeaponAttack(cardPlay.Target))
                .WithHitVfxNode(NBigSlashVfx.Create).WithHitVfxNode(NBigSlashImpactVfx.Create).Execute(choiceContext);

        if (GetOwnerParryAmount(this) > 0m)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(cardPlay.Target),
                DynamicVars.CalculatedBlock.Props, cardPlay);

        int debuffCount = (int)DynamicVars["DebuffCount"].BaseValue;
        foreach (var target in GetSurvivingAttackTargets(attackCmd))
        {
            if (WeaponRewardRuntime.Has(this, 2) && !target.HasPower<LifeLockPower>())
                await PowerCmd.Apply<LifeLockPower>(choiceContext, target,
                    DynamicVars["LifeLockPower"].BaseValue, Owner.Creature, this);
            for (int i = 0; i < debuffCount && target.IsAlive; i++)
                await ApplyRandomDebuff(choiceContext, target);
        }
    }

    private async Task ApplyRandomDebuff(PlayerChoiceContext choiceContext, Creature target)
    {
        switch (Random.Shared.Next(7))
        {
            case 0: await PowerCmd.Apply<VulnerablePower>(choiceContext, target, 2m, Owner.Creature, this); break;
            case 1: await PowerCmd.Apply<WeakPower>(choiceContext, target, 2m, Owner.Creature, this); break;
            case 2: await PowerCmd.Apply<PoisonPower>(choiceContext, target, 5m, Owner.Creature, this); break;
            case 3: await PowerCmd.Apply<DoomPower>(choiceContext, target, 10m, Owner.Creature, this); break;
            case 4: await PowerCmd.Apply<DemisePower>(choiceContext, target, 3m, Owner.Creature, this); break;
            case 5: await PowerCmd.Apply<ImbalancedPower>(choiceContext, target, 1m, Owner.Creature, this); break;
            case 6: await PowerCmd.Apply<StranglePower>(choiceContext, target, 1m, Owner.Creature, this); break;
        }
    }

    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4); }

    public void AddDamage(decimal amount) { DynamicVars.Damage.BaseValue += amount; _totalForged += amount; CurrentDamage = DynamicVars.Damage.BaseValue - 4m * CurrentUpgradeLevel; UpdateDebuffCount(); }
    private void UpdateDebuffCount() => DynamicVars["DebuffCount"].BaseValue =
        Math.Max(1, 1 + (int)(_totalForged / 15)) + (WeaponRewardRuntime.Has(this, 1) ? 1 : 0);
    public void SyncProgressionRewards() { if (IsMutable) UpdateDebuffCount(); }
    protected override void AfterDowngraded() { base.AfterDowngraded(); DynamicVars.Damage.BaseValue = CurrentDamage; }
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
            _vfx = EnsureWeaponVfx("orbit/ApocalypseLongbow.tscn");
        return Task.CompletedTask;
    }
}
