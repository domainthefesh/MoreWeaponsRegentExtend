using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MoreWeaponsRegentExtend.Scripts.Cards;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class KingGunPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add("BulletExtraHit", IsMutable && Owner?.Player is { } player &&
                WeaponProgression.HasReward(player, WeaponKind.SovereignGun, 2));
            description.Add("JadeBonus", IsMutable && Owner?.Player is { } jadePlayer &&
                WeaponProgression.HasReward(jadePlayer, WeaponKind.SovereignGun, 3));
            return description;
        }
    }

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/KingGunPower.png",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/KingGunPower.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner.Player)
            return;

        // 玩家从手牌中选择一张攻击牌（参考 TYRANNY 的选择模式）
        var selected = await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            null,
            this);

        var list = selected.ToList();
        if (list.Count == 0)
            return;

        var card = list[0];

        // 计算卡牌实际伤害（参考 THRASH 的伤害计算模式）
        decimal cardDamage = default;
        if (card.DynamicVars.ContainsKey("CalculatedDamage"))
        {
            cardDamage = card.DynamicVars.CalculatedDamage.Calculate(null);
        }
        else if (card.DynamicVars.ContainsKey("Damage"))
        {
            cardDamage = card.DynamicVars.Damage.BaseValue;
        }
        else if (card.DynamicVars.ContainsKey("OstyDamage"))
        {
            cardDamage = card.DynamicVars.OstyDamage.BaseValue;
        }
        else
        {
            Log.Warn("KingGunPower exhausted attack card " + card.Id.Entry
                + " that did not have an appropriate damage var!");
        }

        cardDamage = Hook.ModifyDamage(
            player.RunState,
            player.Creature.CombatState,
            null,
            player.Creature,
            cardDamage,
            ValueProp.Move,
            card,
            null,
            ModifyDamageHookType.All,
            CardPreviewMode.None,
            out IEnumerable<AbstractModel> _);

        // 加上能力层数（铸造累计值）
        // Bullet damage already includes this same rifle's flat tempering bonus.
        // Other consumed cards retain their own calculated damage, then the rifle
        // adds its own base bonus once to each shot.
        decimal rifleBonus = card is not Bullet &&
            WeaponProgression.HasReward(player, WeaponKind.SovereignGun, 3)
                ? WeaponRewardRuntime.JadeBaseDamageBonus : 0m;
        decimal totalDamage = cardDamage + base.Amount + rifleBonus;

        // 消耗选中的卡牌
        await CardCmd.Exhaust(choiceContext, card);

        // 对随机敌人造成伤害
        var enemies = player.Creature.CombatState!.HittableEnemies;
        if (enemies.Count > 0)
        {
            var target = player.RunState.Rng.CombatCardSelection.NextItem(enemies);
            if (target != null)
            {
                await CreatureCmd.Damage(choiceContext, target, totalDamage,
                    ValueProp.Move, player.Creature);
                if (card is Bullet && target.IsAlive &&
                    WeaponProgression.HasReward(player, WeaponKind.SovereignGun, 2))
                    await CreatureCmd.Damage(choiceContext, target, totalDamage,
                        ValueProp.Move, player.Creature);
            }
        }
    }
}
