using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
using MegaCrit.Sts2.Core.HoverTips;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public class SovereignSpear : MoreWeaponsCardBase
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const decimal _baseDamage = 6m;
    private const decimal _forgeBonus = 4m;

    private decimal _currentDamage = 6m;
    private bool _createdThroughForge;
    private NWeaponVfx? _vfx;

    public override HashSet<CardKeyword> CanonicalKeywords => new() { CardKeyword.Retain };
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => WeaponRewardRuntime.Has(this, 2)
        ? [HoverTipFactory.FromPower<VulnerablePower>()] : [];

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
        new DynamicVar("ForgeBonus", _forgeBonus), new PowerVar<VulnerablePower>(1),
    ];

    public SovereignSpear() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SyncProgressionRewards();
        string animName = "Cast"; float delay = Owner.Character.CastAnimDelay;
        string sfxPath = "event:/sfx/characters/regent/regent_sovereign_blade";
        var cmd = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .WithAttackerAnim(animName, delay).WithAttackerFx(null, sfxPath);

        if (HasSeekingEdge)
            await cmd.TargetingAllOpponents(CombatState!)
                .BeforeDamage(async () =>
                {
                    await ApplyIntentVulnerable(choiceContext, CombatState!.HittableEnemies.ToArray());
                    await TriggerWeaponAttackFirstEnemy();
                })
                .WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3").Execute(choiceContext);
        else
            await cmd.Targeting(cardPlay.Target!)
                .BeforeDamage(async () =>
                {
                    await ApplyIntentVulnerable(choiceContext, [cardPlay.Target!]);
                    await TriggerWeaponAttack(cardPlay.Target);
                })
                .WithHitVfxNode(NBigSlashVfx.Create).WithHitVfxNode(NBigSlashImpactVfx.Create).Execute(choiceContext);

        if (GetOwnerParryAmount(this) > 0m)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(cardPlay.Target),
                DynamicVars.CalculatedBlock.Props, cardPlay);
    }

    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3); }

    public void AddDamage(decimal amount)
    {
        SyncProgressionRewards();
        DynamicVars.Damage.BaseValue += amount + DynamicVars["ForgeBonus"].BaseValue;
        CurrentDamage = DynamicVars.Damage.BaseValue - 3m * CurrentUpgradeLevel;
    }

    public void SyncProgressionRewards()
    {
        if (IsMutable)
            DynamicVars["ForgeBonus"].BaseValue = _forgeBonus + (WeaponRewardRuntime.Has(this, 1) ? 2m : 0m);
    }

    private async Task ApplyIntentVulnerable(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets)
    {
        if (!WeaponRewardRuntime.Has(this, 2)) return;
        foreach (var target in targets.Where(target => target.IsAlive && target.Monster?.IntendsToAttack == true))
            await PowerCmd.Apply<VulnerablePower>(choiceContext, target,
                DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
    }
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
            _vfx = EnsureWeaponVfx("orbit/SovereignSpear.tscn");
        return Task.CompletedTask;
    }
}
