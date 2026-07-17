using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
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

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public class SovereignBludgeon : MoreWeaponsCardBase
{
    private const int energyCost = 3;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const decimal _baseDamage = 20m;

    private decimal _currentDamage = 20m;
    private decimal _currentRepeats = 1m;
    private bool _createdThroughForge;

    public override HashSet<CardKeyword> CanonicalKeywords => new() { CardKeyword.Retain };

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
    ];

    private bool HasSeekingEdge => IsMutable && Owner != null && Owner.Creature.HasPower<SeekingEdgePower>();

    public SovereignBludgeon() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

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
    }

    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(10); }
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

        // 首次进入战斗，或从消耗堆返回：创建/恢复专属 VFX
        if (oldPileType == PileType.None || oldPileType == PileType.Exhaust)
        {
            if (_vfx == null)
            {
                _vfx = NWeaponVfx.Create(this, "SovereignBludgeon.tscn");
                var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner?.Creature);
                creatureNode?.AddChildSafely(_vfx.Root);
                _vfx.Root.Position = Vector2.Zero;
            }
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

        // 卡牌消耗：移除 VFX
        if (card.Pile?.Type == PileType.Exhaust)
        {
            _vfx?.RemoveWeapon();
            _vfx = null;
        }

        return Task.CompletedTask;
    }
}
