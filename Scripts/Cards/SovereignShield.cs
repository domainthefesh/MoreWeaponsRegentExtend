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
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.ValueProps;
using MoreWeaponsRegentExtend.Scripts;
using MoreWeaponsRegentExtend.Scripts.Powers;
using STS2RitsuLib.Keywords;

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
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(MyKeywords.Crystalline)];
    public override bool GainsBlock => true;

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
    ];

    public SovereignShield() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        string animName = "Cast"; float delay = Owner.Character.CastAnimDelay;
        string sfxPath = "event:/sfx/characters/regent/regent_sovereign_blade";

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay).WithAttackerAnim(animName, delay).WithAttackerFx(null, sfxPath)
            .Targeting(cardPlay.Target!)
            .WithHitVfxNode(NBigSlashVfx.Create).WithHitVfxNode(NBigSlashImpactVfx.Create)
            .Execute(choiceContext);

        await PowerCmd.Apply<SovereignShieldPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3); }

    public void AddDamage(decimal amount) { DynamicVars.Block.BaseValue += amount; CurrentBlock = DynamicVars.Block.BaseValue; }

    protected override void AfterDowngraded() { base.AfterDowngraded(); DynamicVars.Block.BaseValue = CurrentBlock; }
    protected override void AfterCloned() { base.AfterCloned(); CreatedThroughForge = false; }

    public override void AfterTransformedFrom()
    {
        _vfx?.RemoveWeapon();
        _vfx = null;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != this) return Task.CompletedTask;
        if (oldPileType == PileType.None || oldPileType == PileType.Exhaust)
        {
            if (_vfx == null)
            {
                _vfx = NWeaponVfx.Create(this, "SovereignShield.tscn");
                var cn = NCombatRoom.Instance?.GetCreatureNode(Owner?.Creature);
                cn?.AddChildSafely(_vfx.Root);
                _vfx.Root.Position = Godot.Vector2.Zero;
            }
        }
        if (card.Pile?.Type == PileType.Exhaust)
        {
            _vfx?.RemoveWeapon();
            _vfx = null;
        }
        return Task.CompletedTask;
    }
}
