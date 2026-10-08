using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public class CrystalDagger : MoreWeaponsCardBase
{
    private const int energyCost = 0;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const decimal _baseDamage = 4m;

    private decimal _currentDamage = 4m;
    private bool _createdThroughForge;

    public override HashSet<CardKeyword> CanonicalKeywords => new() { CardKeyword.Exhaust, CardKeyword.Retain };
    public override TargetType TargetType => HasSeekingEdge ? TargetType.AllEnemies : targetType;
    public override bool GainsBlock => GetOwnerParryAmount(this) > 0m;
    private bool HasSeekingEdge => IsMutable && Owner != null && Owner.Creature.HasPower<SeekingEdgePower>();

    private decimal CurrentDamage { get => _currentDamage; set { AssertMutable(); _currentDamage = value; } }
    public bool CreatedThroughForge { get => _createdThroughForge; set { AssertMutable(); _createdThroughForge = value; } }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(_baseDamage, ValueProp.Move), .. ParryVars()];

    public CrystalDagger() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        string animName = "Cast"; float delay = Owner.Character.CastAnimDelay;
        string sfxPath = "event:/sfx/characters/regent/regent_sovereign_blade";

        var command = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .WithAttackerAnim(animName, delay).WithAttackerFx(null, sfxPath);

        if (HasSeekingEdge)
            await command.TargetingAllOpponents(CombatState!)
                .BeforeDamage(TriggerWeaponAttackFirstEnemy)
                .WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3").Execute(choiceContext);
        else
            await command.Targeting(cardPlay.Target!)
                .BeforeDamage(() => TriggerWeaponAttack(cardPlay.Target))
                .WithHitVfxNode(NBigSlashVfx.Create).WithHitVfxNode(NBigSlashImpactVfx.Create).Execute(choiceContext);

        await GainParryBlock(cardPlay);
    }

    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2); }
    public void AddDamage(decimal amount) { DynamicVars.Damage.BaseValue += amount; CurrentDamage = DynamicVars.Damage.BaseValue; }
    protected override void AfterDowngraded() { base.AfterDowngraded(); DynamicVars.Damage.BaseValue = CurrentDamage; }
    protected override void AfterCloned() { base.AfterCloned(); CreatedThroughForge = false; }

    public override void AfterTransformedFrom()
    {
        RemoveVfxIfAny();
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != this || IsDupe) return Task.CompletedTask;
        if (Pile?.IsCombatPile != true || Pile.Type == PileType.Exhaust)
            RemoveVfxIfAny();
        else
            EnsureWeaponVfx("orbit/CrystalDagger.tscn");
        return Task.CompletedTask;
    }
}
