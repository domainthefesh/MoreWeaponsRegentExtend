using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace MoreWeaponsRegentExtend.Scripts;

public enum WeaponKind
{
    SovereignBludgeon,
    ApocalypseLongbow,
    SovereignCrystalBlade,
    SovereignShield,
    SovereignSpear,
    SovereignAxe,
    SovereignGun,
    SovereignScythe,
    NeptuneTrident,
    SovereignKatana,
    SovereignWings,
    SovereignDagger,
    SeaCalmingStaff,
    GalaxyTrajectoryCannon,
    HolyCodex,
    RoyalBrassKnuckles,
    SovereignBlade
}

public sealed class WeaponSelectionState
{
    public bool Chosen { get; set; }
    public string? Weapon { get; set; }
    public bool EveryForgeRandom { get; set; }
    public bool RandomlySelectedOnce { get; set; }
    public int RandomCounter { get; set; }
}

/// <summary>Per-player armory choice, saved with the run rather than profile settings.</summary>
public static class WeaponSelection
{
    private static PlayerRunSavedData<WeaponSelectionState> _saved = null!;
    private static readonly WeaponKind[] Weapons = Enum.GetValues<WeaponKind>()
        .Where(static weapon => weapon != WeaponKind.SovereignBlade).ToArray();

    public static void Register()
    {
        using (RitsuLibFramework.BeginModDataRegistration(Entry.ModId))
        {
            _saved = RitsuLibFramework.GetRunSavedDataStore(Entry.ModId).RegisterPerPlayer(
                key: "departure_armory",
                defaultFactory: static () => new WeaponSelectionState(),
                options: new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.WhenSet });
        }
    }

    public static bool IsChosen(Player player) => _saved.Get(player).Chosen;

    public static WeaponKind? GetWeapon(Player player)
    {
        var data = _saved.Get(player);
        return data.Chosen && !data.EveryForgeRandom &&
               Enum.TryParse<WeaponKind>(data.Weapon, out var weapon) && Enum.IsDefined(weapon)
            ? weapon
            : null;
    }

    public static bool IsEveryForgeRandom(Player player) => _saved.Get(player).EveryForgeRandom;

    public static WeaponKind? GetHoverWeapon(Player player)
        => _saved.Get(player).RandomlySelectedOnce ? null : GetWeapon(player);

    public static void Select(Player player, WeaponKind weapon)
    {
        if (!Enum.IsDefined(weapon))
            throw new ArgumentOutOfRangeException(nameof(weapon));

        _saved.Modify(player, data =>
        {
            data.Chosen = true;
            data.Weapon = weapon.ToString();
            data.EveryForgeRandom = false;
            data.RandomlySelectedOnce = false;
        });
    }

    public static void SelectRandomOnce(Player player)
    {
        Select(player, RollWeapon(player));
        _saved.Modify(player, data => data.RandomlySelectedOnce = true);
    }

    public static void SelectEveryForge(Player player)
    {
        _saved.Modify(player, data =>
        {
            data.Chosen = true;
            data.Weapon = null;
            data.EveryForgeRandom = true;
            data.RandomlySelectedOnce = false;
        });
    }

    public static WeaponKind RollWeapon(Player player)
    {
        // A separate seeded stream avoids changing card rewards, and resumes after loading.
        var data = _saved.Get(player);
        var rng = new Rng(player.PlayerRng.Seed, Entry.ModId + ".departure_armory");
        for (var i = 0; i < data.RandomCounter; i++)
            rng.NextInt(Weapons.Length);
        var weapon = Weapons[rng.NextInt(Weapons.Length)];
        _saved.Modify(player, state => state.RandomCounter = data.RandomCounter + 1);
        return weapon;
    }
}
