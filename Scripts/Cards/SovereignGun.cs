using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MoreWeaponsRegentExtend.Scripts.Powers;
using STS2RitsuLib.Keywords;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public class SovereignGun : MoreWeaponsCardBase
{
    private const int energyCost = 3;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    private bool _createdThroughForge;

    public override HashSet<CardKeyword> CanonicalKeywords => new() { CardKeyword.Retain };
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<KingGunPower>()];
    public override bool GainsBlock => GetOwnerParryAmount(this) > 0m;
    protected override IEnumerable<DynamicVar> CanonicalVars => ParryVars();

    public bool CreatedThroughForge
    {
        get => _createdThroughForge;
        set { AssertMutable(); _createdThroughForge = value; }
    }

    public SovereignGun() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    // The shared progression cost patch updates this card's base cost on creation and claim.
    public void SyncProgressionRewards() { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        NUpdateCardVfx.Play(this, Owner.Creature);
        await PowerCmd.Apply<KingGunPower>(choiceContext, Owner.Creature, 5m, Owner.Creature, this);
        await GainParryBlock(cardPlay);
    }

    protected override void OnUpgrade() { base.EnergyCost.UpgradeBy(-1); }

    public new void AfterForged() { base.AfterForged(); }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        CreatedThroughForge = false;
    }

    public override void AfterTransformedFrom()
    {
        RemoveVfxIfAny();
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != this || IsDupe) return Task.CompletedTask;
        // 原版铸造特效要求 Damage 变量；铳是能力牌，使用自己的场景。
        if (Pile?.IsCombatPile != true || Pile.Type == PileType.Exhaust)
            RemoveVfxIfAny();
        else
            EnsureWeaponVfx("orbit/SovereignGun.tscn");
        return Task.CompletedTask;
    }
}
