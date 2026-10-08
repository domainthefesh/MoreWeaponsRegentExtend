using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

public sealed class HolyCodex : ForgeableWeaponCardBase
{
    public override int MaxUpgradeLevel => 0;
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, MyKeywords.Blessing];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(MyKeywords.Blessing)];
    protected override IEnumerable<DynamicVar> CanonicalVars => AttackVars(9m,
        new DynamicVar("ForgeStep", 10m), new DynamicVar("Blessings", 1m));

    public HolyCodex() : base(2, CardType.Attack, TargetType.AnyEnemy) { }

    public int Blessings => 1 + (WeaponRewardRuntime.Has(this, 1) ? 1 : 0) + Math.Max(0, (int)(ForgedAmount / 10m));

    public void SyncProgressionRewards() => DynamicVars["Blessings"].BaseValue = Blessings;
    protected override void OnForgeAmountChanged() => SyncProgressionRewards();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SyncProgressionRewards();
        if (WeaponRewardRuntime.Has(this, 2) && WeaponRewardRuntime.IsFirstPlay(this, cardPlay))
            await CreatureCmd.Heal(Owner.Creature, 6m);
        // Snapshot the count before damage hooks can cause another forge.
        int count = Blessings;
        await AttackWithWeaponVfx(choiceContext, cardPlay);
        for (int i = 0; i < count; i++)
            await GrantBlessing(choiceContext, Owner.RunState.Rng.CombatCardGeneration.NextInt(7));
    }

    private async Task GrantBlessing(PlayerChoiceContext choiceContext, int blessing)
    {
        var owner = Owner.Creature;
        switch (blessing)
        {
            case 0: await PowerCmd.Apply<StrengthPower>(choiceContext, owner, 2m, owner, this); break;
            case 1: await PowerCmd.Apply<DexterityPower>(choiceContext, owner, 2m, owner, this); break;
            case 2: await PowerCmd.Apply<RadiancePower>(choiceContext, owner, 2m, owner, this); break;
            case 3: await PowerCmd.Apply<RegenPower>(choiceContext, owner, 3m, owner, this); break;
            case 4: await PowerCmd.Apply<VigorPower>(choiceContext, owner, 6m, owner, this); break;
            case 5: await PowerCmd.Apply<CuriousPower>(choiceContext, owner, 1m, owner, this); break;
            case 6: await PowerCmd.Apply<BarricadePower>(choiceContext, owner, 1m, owner, this); break;
            default: throw new ArgumentOutOfRangeException(nameof(blessing));
        }
    }
}
