using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using MoreWeaponsRegentExtend.Scripts.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Enchantments;

[RegisterEnchantment]
public sealed class Resentment : ModEnchantmentTemplate
{
    public override bool HasExtraCardText => true;
    public override EnchantmentAssetProfile AssetProfile => new(ModelDb.Enchantment<Corrupted>().IconPath);
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ResentmentCursePower>()];

    public override bool CanEnchant(CardModel card)
        => base.CanEnchant(card) && WeaponTaskEnchantments.IsPrimaryWeapon(card);

    public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)
        => props.IsPoweredAttack() ? 2m : 1m;

    // Native OnPlay runs after the card's own effects, including for Power cards
    // that have left the piles. Applying through AfterCardPlayed would miss them.
    public override async Task OnPlay(PlayerChoiceContext context, CardPlay? play)
    {
        if (!HasCard || play == null || !CombatManager.Instance.IsInProgress ||
            !Card.Owner.Creature.IsAlive || Card.Owner.Creature.HasPower<ResentmentCursePower>())
            return;
        await PowerCmd.Apply<ResentmentCursePower>(context, Card.Owner.Creature, 1m,
            Card.Owner.Creature, Card);
    }
}
