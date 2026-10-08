using MegaCrit.Sts2.Core.Models;
using MoreWeaponsRegentExtend.Scripts.Cards;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

/// <summary>Idempotent updates for the legacy weapons after a reward claim or card creation.</summary>
public static class LegacyWeaponRewards
{
    public static void Sync(CardModel card)
    {
        switch (card)
        {
            case SovereignBludgeon hammer: hammer.SyncProgressionRewards(); break;
            case ApocalypseLongbow bow: bow.SyncProgressionRewards(); break;
            case SovereignCrystalBlade crystal: crystal.SyncProgressionRewards(); break;
            case SovereignShield shield: shield.SyncProgressionRewards(); break;
            case SovereignSpear spear: spear.SyncProgressionRewards(); break;
            case SovereignAxe axe: axe.SyncProgressionRewards(); break;
            case SovereignGun gun: gun.SyncProgressionRewards(); break;
            case SovereignScythe scythe: scythe.SyncProgressionRewards(); break;
        }
    }
}
