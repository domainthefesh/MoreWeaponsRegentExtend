using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MoreWeaponsRegentExtend.Scripts.Cards;

namespace MoreWeaponsRegentExtend.Scripts;

internal static class WeaponHoverTips
{
    public static IEnumerable<IHoverTip> ForCard(CardModel card, IEnumerable<IHoverTip> tips)
    {
        var original = tips.ToArray();
        if (!original.Any(static tip => tip is CardHoverTip { Card: SovereignBlade }))
            return original;

        // Owned cards use their own player's choice, including multiplayer. Ownerless
        // reward/library previews use the local run; outside a run the original stays.
        var player = card.IsMutable ? card.Owner : null;
        if (player == null && RunManager.Instance.DebugOnlyGetState() is { } run)
            player = LocalContext.GetMe(run.Players);
        var weapon = player == null ? null : WeaponSelection.GetHoverWeapon(player);
        var result = new List<IHoverTip>(original.Length);
        var replaced = false;
        foreach (var tip in original)
        {
            if (tip is not CardHoverTip { Card: SovereignBlade })
            {
                result.Add(tip);
                continue;
            }

            // Exactly one weapon preview. Other explanations (Forge, Block, Replay,
            // enchantments and card keywords) keep their original order and content.
            if (replaced)
                continue;
            result.Add(weapon.HasValue ? HoverTipFactory.FromCard(GetCanonical(weapon.Value)) : tip);
            replaced = true;
        }
        return result;
    }

    // ModelDb caches canonical models. Only the selected model is requested; each
    // hover gets a fresh preview so UI changes never leak between different cards.
    private static CardModel GetCanonical(WeaponKind weapon) => weapon switch
    {
        WeaponKind.SovereignBludgeon => ModelDb.Card<SovereignBludgeon>(),
        WeaponKind.ApocalypseLongbow => ModelDb.Card<ApocalypseLongbow>(),
        WeaponKind.SovereignCrystalBlade => ModelDb.Card<SovereignCrystalBlade>(),
        WeaponKind.SovereignShield => ModelDb.Card<SovereignShield>(),
        WeaponKind.SovereignSpear => ModelDb.Card<SovereignSpear>(),
        WeaponKind.SovereignAxe => ModelDb.Card<SovereignAxe>(),
        WeaponKind.SovereignGun => ModelDb.Card<SovereignGun>(),
        WeaponKind.SovereignScythe => ModelDb.Card<SovereignScythe>(),
        WeaponKind.NeptuneTrident => ModelDb.Card<NeptuneTrident>(),
        WeaponKind.SovereignKatana => ModelDb.Card<SovereignKatana>(),
        WeaponKind.SovereignWings => ModelDb.Card<SovereignWings>(),
        WeaponKind.SovereignDagger => ModelDb.Card<SovereignDagger>(),
        WeaponKind.SeaCalmingStaff => ModelDb.Card<SeaCalmingStaff>(),
        WeaponKind.GalaxyTrajectoryCannon => ModelDb.Card<GalaxyTrajectoryCannon>(),
        WeaponKind.HolyCodex => ModelDb.Card<HolyCodex>(),
        WeaponKind.RoyalBrassKnuckles => ModelDb.Card<RoyalBrassKnuckles>(),
        _ => ModelDb.Card<SovereignBlade>()
    };
}
