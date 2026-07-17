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
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public bool CreatedThroughForge
    {
        get => _createdThroughForge;
        set { AssertMutable(); _createdThroughForge = value; }
    }

    public SovereignGun() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<KingGunPower>(choiceContext, Owner.Creature, 5m, Owner.Creature, this);
    }

    protected override void OnUpgrade() { base.EnergyCost.UpgradeBy(-1); }

    public void AfterForged() { base.AfterForged(); }

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
        if (card != this) return Task.CompletedTask;
        if ((!CreatedThroughForge && oldPileType == PileType.None) || oldPileType == PileType.Exhaust)
        {
            try { ForgeCmd.PlayCombatRoomForgeVfx(Owner, this); }
            catch (System.Collections.Generic.KeyNotFoundException) { }
        }
        if (card.Pile?.Type == PileType.Exhaust)
            RemoveVfxIfAny();
        return Task.CompletedTask;
    }
}
