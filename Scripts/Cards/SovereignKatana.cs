using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public sealed class SovereignKatana : ForgeableWeaponCardBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => AttackVars(8m, new DynamicVar("ExecuteThreshold", 35m));

    public SovereignKatana() : base(2, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    public void SyncProgressionRewards() => DynamicVars["ExecuteThreshold"].BaseValue =
        WeaponRewardRuntime.Has(this, 2) ? 50m : 35m;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SyncProgressionRewards();
        var attack = await AttackWithWeaponVfx(choiceContext, cardPlay);
        foreach (var target in attack.Results.SelectMany(results => results).Select(result => result.Receiver).Distinct())
        {
            if (target.IsAlive && target.MaxHp > 0
                && target.CurrentHp * 100m < target.MaxHp * DynamicVars["ExecuteThreshold"].BaseValue)
                await CreatureCmd.Kill(target, force: true);
        }
    }
}
