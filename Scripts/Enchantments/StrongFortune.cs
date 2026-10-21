using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Enchantments;

[RegisterEnchantment]
public sealed class StrongFortune : ModEnchantmentTemplate
{
    public override bool HasExtraCardText => true;
    // Reuse an original enchantment icon; no external art dependency is required.
    public override EnchantmentAssetProfile AssetProfile => new(ModelDb.Enchantment<Vigorous>().IconPath);
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamagePercent", 0m)];

    [SavedProperty]
    public int DamagePercent { get; set; }

    [SavedProperty]
    public int LastRolledTurn { get; set; } = -1;

    public override bool CanEnchant(CardModel card)
        => base.CanEnchant(card) && WeaponTaskEnchantments.IsPrimaryWeapon(card);

    public override void RecalculateValues()
        => DynamicVars["DamagePercent"].BaseValue = DamagePercent;

    public void RollForCurrentTurn()
    {
        if (!IsMutable || !HasCard || Card.Owner?.PlayerCombatState is not { } combat)
            return;
        if (LastRolledTurn == combat.TurnNumber)
            return;
        LastRolledTurn = combat.TurnNumber;
        DamagePercent = WeaponTaskEnchantments.RollFortune(Card.Owner);
        RecalculateValues();
    }

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext context, Player player)
    {
        if (HasCard && player == Card.Owner)
            RollForCurrentTurn();
        return Task.CompletedTask;
    }

    public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)
        => props.IsPoweredAttack() ? 1m + DamagePercent / 100m : 1m;
}
