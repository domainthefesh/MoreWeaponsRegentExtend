using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public sealed class SovereignDagger : ForgeableWeaponCardBase
{
    protected override decimal ForgeDamageMultiplier => 0.5m;

    protected override IEnumerable<DynamicVar> CanonicalVars => AttackVars(4m, new DynamicVar("Gold", 15m));

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Fatal)];

    public SovereignDagger() : base(0, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    public void SyncProgressionRewards() => DynamicVars["Gold"].BaseValue =
        WeaponRewardRuntime.Has(this, 1) ? 45m : 15m;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SyncProgressionRewards();
        // Capture Fatal eligibility before damage removes a killed enemy's powers.
        var fatalTargets = GetAttackTargets(cardPlay)
            .Where(target => target.Powers.All(power => power.ShouldOwnerDeathTriggerFatal()))
            .ToHashSet();
        var attack = await AttackWithWeaponVfx(choiceContext, cardPlay);
        int kills = attack.Results.SelectMany(results => results)
            .Where(result => result.WasTargetKilled && fatalTargets.Contains(result.Receiver))
            .Select(result => result.Receiver).Distinct().Count();
        if (kills > 0)
        {
            await PlayerCmd.GainGold(DynamicVars["Gold"].IntValue * kills, Owner);
            NUpdateCardVfx.PlayKillReward(this);
        }
    }
}
