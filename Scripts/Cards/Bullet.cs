using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public class Bullet : MoreWeaponsCardBase
{
    private const int energyCost = 0;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const decimal _baseDamage = 3m;

    private decimal _currentDamage = 3m;
    private bool _createdThroughForge;
    private NWeaponVfx? _vfx;

    public override HashSet<CardKeyword> CanonicalKeywords => new() { CardKeyword.Exhaust };

    private decimal CurrentDamage
    {
        get => _currentDamage;
        set { AssertMutable(); _currentDamage = value; }
    }

    public bool CreatedThroughForge
    {
        get => _createdThroughForge;
        set { AssertMutable(); _createdThroughForge = value; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(_baseDamage, ValueProp.Move),
    ];

    public Bullet() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        string animName = "Cast";
        float delay = Owner.Character.CastAnimDelay;
        string sfxPath = "event:/sfx/characters/regent/regent_sovereign_blade";

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .WithAttackerAnim(animName, delay)
            .WithAttackerFx(null, sfxPath)
            .Targeting(cardPlay.Target!)
            .BeforeDamage(() => TriggerWeaponAttack(cardPlay.Target))
            .WithHitVfxNode(NBigSlashVfx.Create)
            .WithHitVfxNode(NBigSlashImpactVfx.Create)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2); }

    public void AddDamage(decimal amount)
    {
        DynamicVars.Damage.BaseValue += amount;
        CurrentDamage = DynamicVars.Damage.BaseValue;
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue = CurrentDamage;
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        CreatedThroughForge = false;
    }

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
                _vfx = EnsureWeaponVfx("Bullet.tscn");
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
