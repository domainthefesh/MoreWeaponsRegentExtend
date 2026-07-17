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
using STS2RitsuLib.Keywords;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public class SovereignCrystalBlade : MoreWeaponsCardBase
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const decimal _baseDamage = 3m;

    private decimal _currentDamage = 3m;
    private decimal _currentRepeats = 2m;
    private bool _createdThroughForge;
    private NWeaponVfx? _vfx;

    public override HashSet<CardKeyword> CanonicalKeywords => new() { CardKeyword.Retain, MyKeywords.Shatter };
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<CrystalDagger>()];

    public override TargetType TargetType
    {
        get { if (!HasSeekingEdge) return TargetType.AnyEnemy; return TargetType.AllEnemies; }
    }
    public override bool GainsBlock => GetOwnerParryAmount(this) > 0m;
    private bool HasSeekingEdge => IsMutable && Owner != null && Owner.Creature.HasPower<SeekingEdgePower>();

    private decimal CurrentDamage { get => _currentDamage; set { AssertMutable(); _currentDamage = value; } }
    private decimal CurrentRepeats { get => _currentRepeats; set { AssertMutable(); _currentRepeats = value; } }
    public bool CreatedThroughForge { get => _createdThroughForge; set { AssertMutable(); _createdThroughForge = value; } }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(_baseDamage, ValueProp.Move), new RepeatVar(2),
        new CalculationBaseVar(0m), new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => GetOwnerParryAmount(card)),
    ];

    public SovereignCrystalBlade() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        string animName = "Cast"; float delay = Owner.Character.CastAnimDelay;
        string sfxPath = "event:/sfx/characters/regent/regent_sovereign_blade";
        var attackCmd = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .WithHitCount(DynamicVars.Repeat.IntValue).WithAttackerAnim(animName, delay).WithAttackerFx(null, sfxPath);

        if (HasSeekingEdge)
            await attackCmd.TargetingAllOpponents(CombatState!)
                .WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3").Execute(choiceContext);
        else
            await attackCmd.Targeting(cardPlay.Target!)
                .WithHitVfxNode(NBigSlashVfx.Create).WithHitVfxNode(NBigSlashImpactVfx.Create).Execute(choiceContext);

        if (GetOwnerParryAmount(this) > 0m)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(cardPlay.Target),
                DynamicVars.CalculatedBlock.Props, cardPlay);

        var combatState = CombatState!;
        for (int i = 0; i < 3; i++)
        {
            var dagger = combatState.CreateCard<CrystalDagger>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(dagger, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2); }
    public void AddDamage(decimal amount) { DynamicVars.Damage.BaseValue += amount; CurrentDamage = DynamicVars.Damage.BaseValue; }
    public void SetRepeats(decimal amount) { DynamicVars.Repeat.BaseValue = amount; CurrentRepeats = DynamicVars.Repeat.BaseValue; }
    protected override void AfterDowngraded() { base.AfterDowngraded(); DynamicVars.Damage.BaseValue = CurrentDamage; DynamicVars.Repeat.BaseValue = CurrentRepeats; }
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
                _vfx = NWeaponVfx.Create(this, "SovereignCrystalBlade.tscn");
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
