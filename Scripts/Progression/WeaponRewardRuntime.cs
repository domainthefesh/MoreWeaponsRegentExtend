using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MoreWeaponsRegentExtend.Scripts.Cards;
using MoreWeaponsRegentExtend.Scripts.Powers;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

public static class WeaponRewardRuntime
{
    public const decimal JadeBaseDamageBonus = 4m;

    private sealed class CardUse
    {
        public ICombatState? Combat;
        public bool Played;
        public CardPlay? LastPlay;
        public CardPlay? FirstPlay;
        public bool Active;
    }

    private static readonly ConditionalWeakTable<CardModel, CardUse> Uses = new();

    public static bool Has(CardModel card, int node)
        => card.IsMutable && card.Owner is { } player &&
           WeaponProgression.GetWeaponKind(card) is { } kind && WeaponProgression.HasReward(player, kind, node);

    /// <summary>
    /// A flat weapon base bonus, independent of forge scaling. Staff stance scaling is
    /// already baked into its attack value, so its added base damage follows that stance.
    /// Bullets and crystal daggers use their existing parent-weapon reward ownership.
    /// </summary>
    public static decimal GetJadeAttackBonus(CardModel card)
        => card.Type == CardType.Attack && Has(card, 3)
            ? JadeBaseDamageBonus * (card is SeaCalmingStaff staff ? staff.StanceDamageMultiplier : 1m)
            : 0m;

    public static decimal GetBaseAttackBonus(CardModel card)
    {
        decimal bonus = GetJadeAttackBonus(card);
        if (card is SovereignBlade && Has(card, 2))
            bonus += WeaponProgression.GetBladeBaseBonus(card.Owner);
        return bonus;
    }

    public static void BeginPlay(CardPlay play)
    {
        var card = play.Card.DupeOf ?? play.Card;
        var use = Use(card);
        if (use.LastPlay == play)
            return;
        use.LastPlay = play;
        use.Active = true;
        use.FirstPlay = !use.Played && !play.Card.IsDupe && play.IsFirstInSeries ? play : null;
        use.Played = true;
    }

    public static bool IsFirstPlay(CardModel card, CardPlay play)
    {
        BeginPlay(play);
        return !card.IsDupe && Use(card).FirstPlay == play;
    }

    public static bool IsFirstAvailable(CardModel card)
        => !card.IsDupe && (!card.IsMutable || !Use(card).Played);

    public static bool IsFirstDamage(CardModel card, CardPlay? play)
    {
        if (card.IsDupe)
            return false;
        if (play != null)
            return IsFirstPlay(card, play);
        var use = Use(card);
        return use.Active ? use.FirstPlay == use.LastPlay : !use.Played;
    }

    public static void EndPlay(CardPlay play)
    {
        var use = Use(play.Card.DupeOf ?? play.Card);
        if (use.LastPlay == play)
            use.Active = false;
    }

    private static CardUse Use(CardModel card)
    {
        var use = Uses.GetValue(card, static _ => new CardUse());
        if (use.Combat != card.CombatState)
        {
            use.Combat = card.CombatState;
            use.Played = false;
            use.LastPlay = null;
            use.FirstPlay = null;
            use.Active = false;
        }
        return use;
    }

    public static void Sync(CardModel card)
    {
        if (!card.IsMutable || card.Owner == null)
            return;
        LegacyWeaponRewards.Sync(card);
        switch (card)
        {
            case NeptuneTrident trident: trident.SyncProgressionRewards(); break;
            case SovereignKatana katana: katana.SyncProgressionRewards(); break;
            case SovereignWings wings: wings.SyncProgressionRewards(); break;
            case SovereignDagger dagger: dagger.SyncProgressionRewards(); break;
            case HolyCodex codex: codex.SyncProgressionRewards(); break;
            case GalaxyTrajectoryCannon cannon: cannon.RefreshThermalState(); break;
        }
    }

    public static void RefreshCurrentRun()
    {
        if (RunManager.Instance.DebugOnlyGetState() is not { } run)
            return;
        foreach (var player in run.Players)
        {
            if (player.PlayerCombatState is { } combat)
                foreach (var card in combat.AllCards.ToArray())
                {
                    Sync(card);
                    card.InvokeEnergyCostChanged();
                }
            foreach (var power in player.Creature.Powers)
            {
                if (power is HighGroundPower highGround)
                    highGround.SyncProgressionRewards();
                if (power is MartialFanaticPower martial)
                    martial.SyncProgressionRewards();
                if (power is AdaptationPower adaptation)
                    adaptation.SyncProgressionRewards();
            }
        }
    }
}
