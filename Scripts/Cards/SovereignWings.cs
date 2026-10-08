using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MoreWeaponsRegentExtend.Scripts.Powers;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public sealed class SovereignWings : ForgeableWeaponCardBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<HighGroundPower>(2m),
        new DynamicVar("ForgeStep", 10m),
        .. ParryVars()
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<HighGroundPower>()];

    public SovereignWings() : base(2, CardType.Skill, TargetType.Self)
    {
    }

    public void SyncProgressionRewards() =>
        DynamicVars["HighGroundPower"].BaseValue = 2m + (WeaponRewardRuntime.Has(this, 1) ? 1m : 0m)
            + decimal.Floor(ForgedAmount / DynamicVars["ForgeStep"].BaseValue);

    protected override void OnForgeAmountChanged() => SyncProgressionRewards();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SyncProgressionRewards();
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        NUpdateCardVfx.Play(this, Owner.Creature);
        await PowerCmd.Apply<HighGroundPower>(choiceContext, Owner.Creature,
            DynamicVars["HighGroundPower"].BaseValue, Owner.Creature, this);
        await GainParryBlock(cardPlay);
    }
}
