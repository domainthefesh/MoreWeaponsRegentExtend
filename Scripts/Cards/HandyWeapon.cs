using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

[RegisterCard(typeof(RegentCardPool))]
public sealed class HandyWeapon : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://MoreWeaponsRegentExtend/images/cards/HandyWeapon.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [new ForgeVar(3)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => HoverTipFactory.FromForge();

    public HandyWeapon() : base(0, CardType.Skill, CardRarity.Basic, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        NUpdateCardVfx.Play(this, Owner.Creature);
        await ForgeCmd.Forge(DynamicVars.Forge.IntValue, Owner, this);
    }

    protected override void OnUpgrade() => DynamicVars.Forge.UpgradeValueBy(2m);
}
