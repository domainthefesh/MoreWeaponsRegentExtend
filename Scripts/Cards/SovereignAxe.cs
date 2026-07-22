using Godot;
using System.Collections.Generic;
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
using MoreWeaponsRegentExtend.Scripts.Powers;
using STS2RitsuLib.Keywords;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public class SovereignAxe : MoreWeaponsCardBase
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const decimal _baseDamage = 8m;

    private decimal _currentDamage = 8m;
    private bool _createdThroughForge;
    private NWeaponVfx? _vfx;

    public override HashSet<CardKeyword> CanonicalKeywords => new() { CardKeyword.Retain };
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<BleedPower>()];

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
        new CalculationBaseVar(0m), new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => GetOwnerParryAmount(card)),
    ];

    public SovereignAxe() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        string animName = "Cast"; float delay = Owner.Character.CastAnimDelay;
        string sfxPath = "event:/sfx/characters/regent/regent_sovereign_blade";
        var cmd = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .WithAttackerAnim(animName, delay).WithAttackerFx(null, sfxPath);

        if (HasSeekingEdge)
            await cmd.TargetingAllOpponents(CombatState!)
                .BeforeDamage(TriggerWeaponAttackFirstEnemy)
                .WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3").Execute(choiceContext);
        else
            await cmd.Targeting(target)
                .BeforeDamage(() => TriggerWeaponAttack(target))
                .WithHitVfxNode(NBigSlashVfx.Create).WithHitVfxNode(NBigSlashImpactVfx.Create).Execute(choiceContext);

        if (GetOwnerParryAmount(this) > 0m)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(cardPlay.Target),
                DynamicVars.CalculatedBlock.Props, cardPlay);

        await PowerCmd.Apply<BleedPower>(choiceContext, target, 2m, Owner.Creature, this);
    }

    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4); }
    public void AddDamage(decimal amount) { DynamicVars.Damage.BaseValue += amount; CurrentDamage = DynamicVars.Damage.BaseValue; }
    protected override void AfterDowngraded() { base.AfterDowngraded(); DynamicVars.Damage.BaseValue = CurrentDamage; }
    protected override void AfterCloned() { base.AfterCloned(); CreatedThroughForge = false; }

    public override void AfterTransformedFrom()
    {
        RemoveWeaponVfx();
        _vfx = null;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != this) return Task.CompletedTask;
        if (oldPileType == PileType.None || oldPileType == PileType.Exhaust)
        {
            if (_vfx == null)
            {
                _vfx = EnsureWeaponVfx("SovereignAxe.tscn");
            }
        }
        if (card.Pile?.Type == PileType.Exhaust)
        {
            RemoveWeaponVfx();
            _vfx = null;
        }
        return Task.CompletedTask;
    }
}
