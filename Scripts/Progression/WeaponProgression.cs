using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Rooms;
using System.Globalization;
using System.Runtime.CompilerServices;
using MoreWeaponsRegentExtend.Scripts.Cards;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

public enum WeaponTaskStatus
{
    Locked,
    Incomplete,
    Claimable,
    Claimed
}

/// <summary>Only this run owns the milestones and claimed weapon rewards.</summary>
public sealed class WeaponProgressionState
{
    public bool FirstBossDefeated { get; set; }
    public bool SecondBossDefeated { get; set; }
    public Dictionary<string, int> ClaimedRewards { get; set; } = new();
    public decimal SovereignBladeBaseBonus { get; set; }
    public int EliteVictories { get; set; }
    public HashSet<string> CountedEliteVictories { get; set; } = new();
}

public static class WeaponProgression
{
    public const int EliteVictoriesRequired = 5;
    private static PlayerRunSavedData<WeaponProgressionState>? _saved;
    private static IDisposable? _victorySubscription;
    private static readonly ConditionalWeakTable<CombatRoom, EliteVictoryLocation> EliteLocations = new();
    private sealed record EliteVictoryLocation(string Key);

    /// <summary>UI and cards refresh after milestone, claim or sword-growth changes.</summary>
    public static event Action? Changed;

    public static void Register()
    {
        if (_saved != null)
            return;

        using (RitsuLibFramework.BeginModDataRegistration(Entry.ModId))
        {
            _saved = RitsuLibFramework.GetRunSavedDataStore(Entry.ModId).RegisterPerPlayer(
                key: "weapon_progression",
                defaultFactory: static () => new WeaponProgressionState(),
                options: new RunSavedDataOptions
                {
                    SchemaVersion = 1,
                    WritePolicy = RunSavedDataWritePolicy.WhenSet
                });
        }

        _victorySubscription = RitsuLibFramework.SubscribeLifecycle<CombatVictoryEvent>(RecordVictory);
        WeaponClaimNetwork.Register();
    }

    public static bool IsEligible(Player player, WeaponKind weapon)
        => player.Character is Regent && Enum.IsDefined(weapon) &&
           (WeaponSelection.IsEveryForgeRandom(player) || WeaponSelection.GetWeapon(player) == weapon);

    public static WeaponTaskStatus GetNodeStatus(Player player, WeaponKind weapon, int node)
    {
        ValidateNode(node);
        if (_saved == null || !IsEligible(player, weapon))
            return WeaponTaskStatus.Locked;

        var state = _saved.Get(player);
        if (HasClaim(state, weapon, node))
            return WeaponTaskStatus.Claimed;
        if (node == 2 && !HasClaim(state, weapon, 1))
            return WeaponTaskStatus.Locked;
        return IsNodeComplete(state, node)
            ? WeaponTaskStatus.Claimable
            : WeaponTaskStatus.Incomplete;
    }

    public static bool HasReward(Player player, WeaponKind weapon, int node)
    {
        ValidateNode(node);
        return _saved != null && HasClaim(_saved.Get(player), weapon, node);
    }

    /// <summary>Returns false for unearned, ineligible, out-of-order or duplicate claims.</summary>
    public static bool TryClaim(Player player, WeaponKind weapon, int node)
    {
        if (CombatManager.Instance.IsInProgress ||
            GetNodeStatus(player, weapon, node) != WeaponTaskStatus.Claimable)
            return false;

        _saved!.Modify(player, state =>
        {
            var key = weapon.ToString();
            state.ClaimedRewards.TryGetValue(key, out var claims);
            state.ClaimedRewards[key] = claims | 1 << (node - 1);
        });
        Changed?.Invoke();
        return true;
    }

    /// <summary>UI entry: multiplayer claims follow the trusted sender through Ritsu's host relay.</summary>
    public static bool RequestClaim(Player player, WeaponKind weapon, int node)
        => WeaponClaimNetwork.RequestClaim(player, weapon, node);

    public static bool IsBossDefeated(Player player, int node)
    {
        if (node is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(node));
        if (_saved == null)
            return false;
        var state = _saved.Get(player);
        return node == 1 ? state.FirstBossDefeated : state.SecondBossDefeated;
    }

    public static int GetEliteVictories(Player player)
        => Math.Max(0, _saved?.Get(player).EliteVictories ?? 0);

    public static bool IsNodeComplete(Player player, int node)
    {
        ValidateNode(node);
        return _saved != null && IsNodeComplete(_saved.Get(player), node);
    }

    private static bool IsNodeComplete(WeaponProgressionState state, int node) => node switch
    {
        1 => state.FirstBossDefeated,
        2 => state.SecondBossDefeated,
        3 => state.EliteVictories >= EliteVictoriesRequired,
        _ => false
    };

    public static decimal GetBladeBaseBonus(Player player)
        => _saved?.Get(player).SovereignBladeBaseBonus ?? 0;

    public static void AddBladeBaseBonus(Player player, decimal amount)
    {
        if (_saved == null || amount <= 0 || !HasReward(player, WeaponKind.SovereignBlade, 2))
            return;
        _saved.Modify(player, state => state.SovereignBladeBaseBonus += amount);
        Changed?.Invoke();
    }

    /// <summary>Native victory events exclude defeat, abandonment and ordinary fights.</summary>
    public static void RecordVictory(CombatVictoryEvent victory)
    {
        if (_saved == null)
            return;
        if (victory.Room.RoomType == RoomType.Elite)
        {
            RecordEliteVictory(victory);
            return;
        }
        if (victory.Room.RoomType != RoomType.Boss ||
            victory.RunState.CurrentActIndex is not (0 or 1))
            return;

        var node = victory.RunState.CurrentActIndex + 1;
        var changed = false;
        foreach (var player in victory.RunState.Players.Where(static player => player.Character is Regent))
        {
            if (IsBossDefeated(player, node))
                continue;
            _saved.Modify(player, state =>
            {
                if (node == 1)
                    state.FirstBossDefeated = true;
                else
                    state.SecondBossDefeated = true;
            });
            changed = true;
        }
        if (changed)
            Changed?.Invoke();
    }

    private static void RecordEliteVictory(CombatVictoryEvent victory)
    {
        // Capture the location once per actual room; a delayed repeat must not use
        // a later map point. Persisted keys also reject repeats after loading.
        var location = EliteLocations.GetValue(victory.Room,
            _ => new EliteVictoryLocation(GetEliteVictoryKey(victory))).Key;
        var changed = false;
        foreach (var player in victory.RunState.Players.Where(static player => player.Character is Regent))
        {
            var state = _saved!.Get(player);
            if (state.CountedEliteVictories.Contains(location))
                continue;
            _saved.Modify(player, data =>
            {
                if (data.CountedEliteVictories.Add(location))
                    data.EliteVictories++;
            });
            changed = true;
        }
        if (changed)
            Changed?.Invoke();
    }

    private static string GetEliteVictoryKey(CombatVictoryEvent victory)
    {
        var run = victory.RunState;
        var coord = run.CurrentMapCoord;
        var roomId = victory.Room.Id;
        // Entered rooms use native locations, including a room ID for room stacks.
        // Unentered console/test rooms have neither. Their actual room object still
        // deduplicates within this process; deterministic ordinals keep distinct
        // victories separate without random IDs or collapsing every null location.
        if (coord == null && roomId == null)
        {
            var ordinal = run.Players.Where(static player => player.Character is Regent)
                .Select(player => _saved!.Get(player).EliteVictories).DefaultIfEmpty(0).Max() + 1;
            return FormattableString.Invariant($"unlocated|{run.CurrentActIndex}|{ordinal}");
        }
        var mapKey = coord is { } map
            ? FormattableString.Invariant($"{map.col},{map.row}") : "none";
        return string.Join("|", run.CurrentActIndex.ToString(CultureInfo.InvariantCulture), mapKey,
            roomId?.ToString(CultureInfo.InvariantCulture) ?? "none");
    }

    public static WeaponKind? GetWeaponKind(CardModel card) => card switch
    {
        SovereignBludgeon => WeaponKind.SovereignBludgeon,
        ApocalypseLongbow => WeaponKind.ApocalypseLongbow,
        SovereignCrystalBlade or CrystalDagger => WeaponKind.SovereignCrystalBlade,
        SovereignShield => WeaponKind.SovereignShield,
        SovereignSpear => WeaponKind.SovereignSpear,
        SovereignAxe => WeaponKind.SovereignAxe,
        SovereignGun or Bullet => WeaponKind.SovereignGun,
        SovereignScythe => WeaponKind.SovereignScythe,
        NeptuneTrident => WeaponKind.NeptuneTrident,
        SovereignKatana => WeaponKind.SovereignKatana,
        SovereignWings => WeaponKind.SovereignWings,
        SovereignDagger => WeaponKind.SovereignDagger,
        SeaCalmingStaff => WeaponKind.SeaCalmingStaff,
        GalaxyTrajectoryCannon or CannonCharge => WeaponKind.GalaxyTrajectoryCannon,
        HolyCodex => WeaponKind.HolyCodex,
        RoyalBrassKnuckles => WeaponKind.RoyalBrassKnuckles,
        SovereignBlade => WeaponKind.SovereignBlade,
        _ => null
    };

    private static bool HasClaim(WeaponProgressionState state, WeaponKind weapon, int node)
        => state.ClaimedRewards.TryGetValue(weapon.ToString(), out var claims) &&
           (claims & 1 << (node - 1)) != 0;

    private static void ValidateNode(int node)
    {
        if (node is not (1 or 2 or 3))
            throw new ArgumentOutOfRangeException(nameof(node));
    }
}
