using Godot;
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
using MoreWeaponsRegentExtend.Scripts.Powers;
using STS2RitsuLib.Keywords;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public class SovereignShield : MoreWeaponsCardBase
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const decimal _baseBlock = 5m;

    private decimal _currentBlock = 5m;
    private bool _createdThroughForge;
    private NWeaponVfx? _vfx;

    public override HashSet<CardKeyword> CanonicalKeywords => new() { CardKeyword.Retain, MyKeywords.Crystalline };
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => WeaponRewardRuntime.Has(this, 1)
        ? [] : [HoverTipFactory.FromKeyword(MyKeywords.Crystalline)];
    public override bool GainsBlock => true;
    private bool HasSeekingEdge => IsMutable && Owner != null && Owner.Creature.HasPower<SeekingEdgePower>();
    public override TargetType TargetType => HasSeekingEdge ? TargetType.AllEnemies : TargetType.AnyEnemy;

    private decimal CurrentBlock
    {
        get => _currentBlock;
        set { AssertMutable(); _currentBlock = value; }
    }

    public bool CreatedThroughForge
    {
        get => _createdThroughForge;
        set { AssertMutable(); _createdThroughForge = value; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(_baseBlock, ValueProp.Move),
        new CalculationBaseVar(0m), new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => card.Owner.Creature.Block),
        new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => GetOwnerParryAmount(card)),
        new BlockVar("FirstBlock", _baseBlock * 2m, ValueProp.Move),
    ];

    public SovereignShield() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SyncProgressionRewards();
        bool firstPlay = WeaponRewardRuntime.IsFirstPlay(this, cardPlay);
        decimal ownBlock = DynamicVars.Block.BaseValue *
            (firstPlay && WeaponRewardRuntime.Has(this, 2) ? 2m : 1m);
        await CreatureCmd.GainBlock(Owner.Creature, ownBlock, DynamicVars.Block.Props, cardPlay);

        string animName = "Cast"; float delay = Owner.Character.CastAnimDelay;
        string sfxPath = "event:/sfx/characters/regent/regent_sovereign_blade";

        var attack = DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay).WithAttackerAnim(animName, delay).WithAttackerFx(null, sfxPath);
        if (HasSeekingEdge)
            await attack.TargetingAllOpponents(CombatState!)
                .BeforeDamage(TriggerWeaponAttackFirstEnemy)
                .WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3").Execute(choiceContext);
        else
            await attack.Targeting(cardPlay.Target!)
                .BeforeDamage(() => TriggerWeaponAttack(cardPlay.Target))
                .WithHitVfxNode(NBigSlashVfx.Create).WithHitVfxNode(NBigSlashImpactVfx.Create)
                .Execute(choiceContext);

        if (!WeaponRewardRuntime.Has(this, 1))
            await PowerCmd.Apply<SovereignShieldPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        await GainParryBlock(cardPlay);
    }

    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3); }

    public void AddDamage(decimal amount) { DynamicVars.Block.BaseValue += amount; CurrentBlock = DynamicVars.Block.BaseValue - 3m * CurrentUpgradeLevel; SyncProgressionRewards(); }

    public void SyncProgressionRewards()
    {
        if (!IsMutable) return;
        DynamicVars["FirstBlock"].BaseValue = DynamicVars.Block.BaseValue * 2m;
        if (WeaponRewardRuntime.Has(this, 1) && Keywords.Contains(MyKeywords.Crystalline))
            RemoveKeyword(MyKeywords.Crystalline);
    }

    protected override void AfterDowngraded() { base.AfterDowngraded(); DynamicVars.Block.BaseValue = CurrentBlock; SyncProgressionRewards(); }
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
            _vfx = EnsureWeaponVfx("orbit/SovereignShield.tscn");
        return Task.CompletedTask;
    }
}
