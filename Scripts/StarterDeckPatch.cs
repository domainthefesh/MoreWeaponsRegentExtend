using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MoreWeaponsRegentExtend.Scripts.Cards;
using STS2RitsuLib.Patching.Models;

namespace MoreWeaponsRegentExtend.Scripts;

public sealed class HandyWeaponStarterDeckPatch : IPatchMethod
{
    public static string PatchId => "more_weapons_handy_weapon_starter";
    public static string Description => "Replace one Regent Strike with Handy Weapon";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() => [new(typeof(Regent), "get_StartingDeck")];

    public static void Postfix(ref IEnumerable<CardModel> __result)
    {
        var deck = __result.ToList();
        if (deck.Any(card => card is HandyWeapon))
            return;

        int index = deck.FindIndex(card => card is StrikeRegent);
        if (index >= 0)
            deck[index] = ModelDb.Card<HandyWeapon>();

        __result = deck;
    }
}
