using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

// 抽象基类：统一管理卡图路径，所有子类自动注册到 TokenCardPool
[RegisterCard(typeof(TokenCardPool), Inherit = true)]
public abstract class MoreWeaponsCardBase : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://MoreWeaponsRegentExtend/images/cards/{GetType().Name}.png",
        FramePath: this.Type switch
        {
            CardType.Attack => "res://MoreWeaponsRegentExtend/images/card_frame_attack.png",
            CardType.Skill => "res://MoreWeaponsRegentExtend/images/card_frame_skill.png",
            CardType.Power => "res://MoreWeaponsRegentExtend/images/card_frame_power.png",
            _ => ""
        }
    );

    protected MoreWeaponsCardBase(int energyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary)
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // === ParryPower 格挡计算（子类共用） ===
    protected static decimal GetOwnerParryAmount(CardModel card)
    {
        if (!card.IsMutable || card.Owner == null || card.Pile == null || !card.Pile.IsCombatPile)
            return 0m;
        var amount = card.Owner.Creature.GetPowerAmount<ParryPower>();
        return amount;
    }

    // === VFX 清理：尝试移除浮空剑 VFX ===
    protected void RemoveVfxIfAny()
    {
        try
        {
            if (Owner != null)
                SovereignBlade.GetVfxNode(Owner, this)?.RemoveSovereignBlade();
        }
        catch { /* VFX 清理失败不抛异常 */ }
    }
}
