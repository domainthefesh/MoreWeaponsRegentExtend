using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MoreWeaponsRegentExtend.Scripts.Cards;
using MoreWeaponsRegentExtend.Scripts.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace MoreWeaponsRegentExtend.Scripts;

// 铸造单例：监听 AfterForge 事件，为所有锻造卡牌增加数值并播放铸造动画
[RegisterSingleton]
public class ForgeSingleton : HookedSingletonModel
{
    // 全局累计锻造数值（供 KingGunPower 等参考）
    public static decimal TotalForgedAmount { get; private set; }

    public ForgeSingleton() : base(HookType.Combat) { }

    public override async Task AfterForge(decimal amount, Player forger, AbstractModel? source)
    {
        TotalForgedAmount += amount;

        foreach (var card in forger.PlayerCombatState!.AllCards)
        {
            if (card is SovereignBludgeon bludgeon)
            {
                bludgeon.AddDamage(amount);
                bludgeon.AfterForged();
                // 使用专属 VFX，不走 PlayCombatRoomForgeVfx
            }
            else if (card is ApocalypseLongbow longbow)
            {
                longbow.AddDamage(amount);
                longbow.AfterForged();
            }
            else if (card is SovereignCrystalBlade crystalBlade)
            {
                crystalBlade.AddDamage(amount);
                crystalBlade.AfterForged();
            }
            else if (card is CrystalDagger dagger)
            {
                dagger.AddDamage(amount);
                dagger.AfterForged();
                ForgeCmd.PlayCombatRoomForgeVfx(forger, dagger);
            }
            else if (card is SovereignShield shield)
            {
                shield.AddDamage(amount);
                shield.AfterForged();
            }
            else if (card is SovereignSpear spear)
            {
                spear.AddDamage(amount);
                spear.AfterForged();
            }
            else if (card is SovereignAxe axe)
            {
                axe.AddDamage(amount);
                axe.AfterForged();
            }
            else if (card is SovereignScythe scythe)
            {
                scythe.AddDamage(amount);
                scythe.AfterForged();
            }
            else if (card is Bullet bullet)
            {
                bullet.AddDamage(amount);
                bullet.AfterForged();
            }
        }

        // 铸造增加王之枪层数
        var gunPower = forger.Creature.GetPower<KingGunPower>();
        if (gunPower != null)
        {
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), gunPower, amount, null, null);
        }

        // 卡牌锻造闪光特效（NCardSmithVfx）
        PlayCardSmithVfx(forger);
    }

    private static void PlayCardSmithVfx(Player forger)
    {
        var modCards = forger.PlayerCombatState!.AllCards
            .Where(c => !c.IsDupe && ForgeRandomizePatch.IsModForgeCard(c))
            .ToList();

        if (modCards.Count == 0) return;

        // 手牌中的卡牌：逐张播放闪光
        var handCards = modCards.Where(c => c.Pile?.Type == PileType.Hand).ToList();
        foreach (var card in handCards)
        {
            var nCard = NCombatRoom.Instance?.Ui?.Hand?.GetCard(card);
            if (nCard != null)
            {
                var vfx = NCardSmithVfx.Create(nCard, playSfx: false);
                NRun.Instance?.GlobalUi?.AboveTopBarVfxContainer?.AddChildSafely(vfx);
            }
        }

        // 非手牌中的卡牌：预览闪光
        var otherCards = modCards.Where(c => c.Pile?.Type != PileType.Hand).ToList();
        if (otherCards.Count > 0)
        {
            var vfx = NCardSmithVfx.Create(otherCards, playSfx: false);
            NRun.Instance?.GlobalUi?.CardPreviewContainer?.AddChildSafely(vfx);
        }
    }
}
