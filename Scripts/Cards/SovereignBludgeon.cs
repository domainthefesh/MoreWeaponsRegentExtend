using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Godot;
using STS2RitsuLib.Keywords;
using MoreWeaponsRegentExtend.Scripts.Powers;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public class SovereignBludgeon : MoreWeaponsCardBase
{
    private const int energyCost = 3;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const decimal _baseDamage = 15m;

    private decimal _currentDamage = 15m;
    private decimal _totalForged;
    private decimal _currentRepeats = 1m;
    private bool _createdThroughForge;
    private decimal _progressionDamageBonus;

    public override HashSet<CardKeyword> CanonicalKeywords => new() { CardKeyword.Retain };
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => WeaponRewardRuntime.Has(this, 1)
        ? [HoverTipFactory.FromPower<HeavyPressurePower>(), HoverTipFactory.FromPower<ShrinkPower>()]
        : [HoverTipFactory.FromPower<HeavyPressurePower>()];
    public decimal LocalPressureThreshold => 10m + _totalForged;
    public decimal PressureThreshold => SharedHammerPressure.Observe(this);

    public void SyncPressureThreshold(decimal threshold)
    {
        AssertMutable();
        DynamicVars["PressureThreshold"].BaseValue = threshold;
    }

    public override TargetType TargetType
    {
        get { if (!HasSeekingEdge) return TargetType.AnyEnemy; return TargetType.AllEnemies; }
    }
    public override bool GainsBlock => GetOwnerParryAmount(this) > 0m;

    private decimal CurrentDamage { get => _currentDamage; set { AssertMutable(); _currentDamage = value; } }
    private decimal CurrentRepeats { get => _currentRepeats; set { AssertMutable(); _currentRepeats = value; } }
    public bool CreatedThroughForge { get => _createdThroughForge; set { AssertMutable(); _createdThroughForge = value; } }

    // === 专属 VFX ===
    private NWeaponVfx? _vfx;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(_baseDamage, ValueProp.Move),
        new CalculationBaseVar(0m), new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => GetOwnerParryAmount(card)),
        new RepeatVar(1),
        new PowerVar<HeavyPressurePower>(2),
        new DynamicVar("PressureThreshold", 10m),
        new PowerVar<ShrinkPower>(1),
    ];

    private bool HasSeekingEdge => IsMutable && Owner != null && Owner.Creature.HasPower<SeekingEdgePower>();

    public SovereignBludgeon() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SyncProgressionRewards();
        string animName = "Cast"; float delay = Owner.Character.CastAnimDelay;
        string sfxPath = "event:/sfx/characters/regent/regent_sovereign_blade";

        var attackCmd = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .WithHitCount(DynamicVars.Repeat.IntValue).WithAttackerAnim(animName, delay).WithAttackerFx(null, sfxPath);

        if (HasSeekingEdge)
            await attackCmd.TargetingAllOpponents(CombatState!)
                .BeforeDamage(TriggerWeaponAttackFirstEnemy)
                .WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3").Execute(choiceContext);
        else
            await attackCmd.Targeting(cardPlay.Target!)
                .BeforeDamage(() => TriggerWeaponAttack(cardPlay.Target))
                .WithHitVfxNode(NBigSlashVfx.Create).WithHitVfxNode(NBigSlashImpactVfx.Create).Execute(choiceContext);

        foreach (var target in GetSurvivingAttackTargets(attackCmd))
        {
            await PowerCmd.Apply<HeavyPressurePower>(choiceContext, target,
                DynamicVars["HeavyPressurePower"].BaseValue, Owner.Creature, this);
            if (WeaponRewardRuntime.Has(this, 1) && target.IsAlive)
                await PowerCmd.Apply<ShrinkPower>(choiceContext, target,
                    DynamicVars["ShrinkPower"].BaseValue, Owner.Creature, this);
        }

        if (GetOwnerParryAmount(this) > 0m)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(cardPlay.Target),
                DynamicVars.CalculatedBlock.Props, cardPlay);
    }

    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(10); }
    public void SyncProgressionRewards()
    {
        if (!IsMutable) return;
        decimal bonus = WeaponRewardRuntime.Has(this, 2) ? 10m : 0m;
        if (bonus != _progressionDamageBonus)
        {
            DynamicVars.Damage.BaseValue += bonus - _progressionDamageBonus;
            _progressionDamageBonus = bonus;
        }
        CurrentDamage = DynamicVars.Damage.BaseValue - 10m * CurrentUpgradeLevel;
    }
    public void AddDamage(decimal amount)
    {
        AssertMutable();
        DynamicVars.Damage.BaseValue += amount;
        _totalForged += amount;
        CurrentDamage = DynamicVars.Damage.BaseValue - 10m * CurrentUpgradeLevel;
        SyncPressureThreshold(PressureThreshold);
        SyncProgressionRewards();
    }
    public void SetRepeats(decimal amount) { DynamicVars.Repeat.BaseValue = amount; CurrentRepeats = DynamicVars.Repeat.BaseValue; }
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue = CurrentDamage;
        DynamicVars.Repeat.BaseValue = CurrentRepeats;
        SyncPressureThreshold(PressureThreshold);
        SyncProgressionRewards();
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
        SyncPressureThreshold(PressureThreshold);
        if (Pile?.IsCombatPile != true || Pile.Type == PileType.Exhaust)
        {
            RemoveWeaponVfx();
            _vfx = null;
            return Task.CompletedTask;
        }
        _vfx = EnsureWeaponVfx("orbit/SovereignBludgeon.tscn");

        // 首次进入战斗，或从消耗堆返回：创建/恢复专属 VFX
        if (oldPileType == PileType.None || oldPileType == PileType.Exhaust)
        {
            // 锻造闪光特效
            NCardSmithVfx? cardVfx = null;
            if (card.Pile?.Type == PileType.Hand)
            {
                var nCard = NCombatRoom.Instance?.Ui?.Hand?.GetCard(this);
                if (nCard != null)
                    cardVfx = NCardSmithVfx.Create(nCard, playSfx: false);
            }
            if (cardVfx != null)
                NRun.Instance?.GlobalUi?.AboveTopBarVfxContainer?.AddChildSafely(cardVfx);
        }

        return Task.CompletedTask;
    }
}
