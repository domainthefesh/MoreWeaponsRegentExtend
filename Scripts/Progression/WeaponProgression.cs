using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using MoreWeaponsRegentExtend.Scripts.Cards;
using STS2RitsuLib;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

public enum WeaponTaskStatus
{
    Locked,
    Incomplete,
    Claimable,
    Claimed,
    ExclusiveLocked
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
    public int UpgradeCount { get; set; }
    public int QuickEliteVictories { get; set; }
    public int EventsCompleted { get; set; }
    public int UnknownCombatVictories { get; set; }
    public int CombatVictories { get; set; }
    public HashSet<string> CountedCombatVictories { get; set; } = new();
    public HashSet<string> CountedEvents { get; set; } = new();
}

public static class WeaponProgression
{
    public const int EliteVictoriesRequired = 5;
    public const int UpgradeCountRequired = 5;
    public const int QuickEliteVictoriesRequired = 3;
    public const int QuickEliteMaxRound = 4;
    public const int EventsCompletedRequired = 5;
    public const int UnknownCombatVictoriesRequired = 3;
    public const int CombatVictoriesRequired = 18;
    public const int LastNode = 9;
    private static PlayerRunSavedData<WeaponProgressionState>? _saved;
    private static IDisposable? _victorySubscription;
    private static readonly List<IDisposable> TrackingSubscriptions = new();
    private static readonly ConditionalWeakTable<AbstractRoom, RoomLocation> RoomLocations = new();
    private sealed record RoomLocation(string Key, bool IsUnknown);

    private sealed class ProgressionMigration : IMigration
    {
        public int FromVersion => 1;
        public int ToVersion => 2;
        // Existing fields and reward bits remain unchanged. Missing additive fields
        // deserialize to the defaults declared on WeaponProgressionState.
        public bool Migrate(JsonObject data) => true;
    }

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
                    SchemaVersion = 2,
                    Migrations = [new ProgressionMigration()],
                    WritePolicy = RunSavedDataWritePolicy.WhenSet
                });
        }

        _victorySubscription = RitsuLibFramework.SubscribeLifecycle<CombatVictoryEvent>(RecordVictory);
        TrackingSubscriptions.Add(RitsuLibFramework.SubscribeLifecycle<RoomEnteredEvent>(RecordRoomEntered));
        TrackingSubscriptions.Add(RitsuLibFramework.SubscribeLifecycle<RoomExitedEvent>(RecordRoomExited));
        TrackingSubscriptions.Add(RitsuLibFramework.SubscribeLifecycle<RelicObtainedEvent>(relic =>
        {
            if (relic.Relic is JuzuBracelet)
                Changed?.Invoke();
        }));
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
        if ((node == 5 && HasClaim(state, weapon, 6)) ||
            (node == 6 && HasClaim(state, weapon, 5)))
            return WeaponTaskStatus.ExclusiveLocked;
        if (node == 2 && !HasClaim(state, weapon, 1))
            return WeaponTaskStatus.Locked;
        return IsNodeComplete(player, node)
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
        if (!TryClaimCore(player, weapon, node))
            return false;
        if (node == 7)
            TaskHelper.RunSafely(FinishClaimRewardAsync(player, weapon, node));
        return true;
    }

    public static async Task<bool> TryClaimAsync(Player player, WeaponKind weapon, int node)
    {
        if (!TryClaimCore(player, weapon, node))
            return false;
        await FinishClaimRewardAsync(player, weapon, node);
        return true;
    }

    private static bool TryClaimCore(Player player, WeaponKind weapon, int node)
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

    private static async Task FinishClaimRewardAsync(Player player, WeaponKind weapon, int node)
    {
        if (node != 7 || player.GetRelic<FencingManual>() != null)
            return;
        try
        {
            await RelicCmd.Obtain<FencingManual>(player);
        }
        catch
        {
            // Failed relic acquisition must not permanently consume the claim.
            _saved!.Modify(player, state => state.ClaimedRewards[weapon.ToString()] &= ~(1 << (node - 1)));
            Changed?.Invoke();
            throw;
        }
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
        return _saved != null && (node == 8 && player.GetRelic<JuzuBracelet>() != null ||
            IsNodeComplete(_saved.Get(player), node));
    }

    private static bool IsNodeComplete(WeaponProgressionState state, int node) => node switch
    {
        1 => state.FirstBossDefeated,
        2 => state.SecondBossDefeated,
        3 => state.EliteVictories >= EliteVictoriesRequired,
        4 => state.UpgradeCount >= UpgradeCountRequired,
        5 or 6 => state.FirstBossDefeated,
        7 => state.QuickEliteVictories >= QuickEliteVictoriesRequired,
        8 => state.EventsCompleted >= EventsCompletedRequired,
        9 => state.UnknownCombatVictories >= UnknownCombatVictoriesRequired ||
             state.CombatVictories >= CombatVictoriesRequired,
        _ => false
    };

    public static (int Current, int Required) GetNodeProgress(Player player, int node)
    {
        ValidateNode(node);
        var state = _saved?.Get(player);
        return node switch
        {
            1 or 5 or 6 => (state?.FirstBossDefeated == true ? 1 : 0, 1),
            2 => (state?.SecondBossDefeated == true ? 1 : 0, 1),
            3 => (Math.Max(0, state?.EliteVictories ?? 0), EliteVictoriesRequired),
            4 => (Math.Max(0, state?.UpgradeCount ?? 0), UpgradeCountRequired),
            7 => (Math.Max(0, state?.QuickEliteVictories ?? 0), QuickEliteVictoriesRequired),
            8 => (Math.Max(0, state?.EventsCompleted ?? 0), EventsCompletedRequired),
            9 => (Math.Max(0, state?.UnknownCombatVictories ?? 0), UnknownCombatVictoriesRequired),
            _ => (0, 0)
        };
    }

    public static (int Current, int Required) GetNodeAlternativeProgress(Player player, int node)
    {
        ValidateNode(node);
        return node switch
        {
            8 => (player.GetRelic<JuzuBracelet>() != null ? 1 : 0, 1),
            9 => (Math.Max(0, _saved?.Get(player).CombatVictories ?? 0), CombatVictoriesRequired),
            _ => (0, 0)
        };
    }

    public static string GetNodeProgressText(Player player, int node)
    {
        var progress = GetNodeProgress(player, node);
        return FormattableString.Invariant($"{progress.Current} / {progress.Required}");
    }

    public static void RecordCardUpgrade(CardModel card, int successfulLevels)
    {
        if (_saved == null || successfulLevels <= 0 || !card.IsMutable ||
            card.Owner is not { Character: Regent } player)
            return;
        _saved.Modify(player, state => state.UpgradeCount += successfulLevels);
        Changed?.Invoke();
    }

    // The console command separately enforces singleplayer and no active combat.
    public static void SetDebugProgress(Player player, int node, int value, bool alternative = false)
    {
        ValidateNode(node);
        if (_saved == null || value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        _saved.Modify(player, state =>
        {
            switch (node)
            {
                case 1: case 5: case 6: state.FirstBossDefeated = value > 0; break;
                case 2: state.SecondBossDefeated = value > 0; break;
                case 3: state.EliteVictories = value; break;
                case 4: state.UpgradeCount = value; break;
                case 7: state.QuickEliteVictories = value; break;
                case 8: state.EventsCompleted = value; break;
                case 9 when alternative: state.CombatVictories = value; break;
                case 9: state.UnknownCombatVictories = value; break;
            }
        });
        Changed?.Invoke();
    }

    public static decimal GetBladeBaseBonus(Player player)
        => _saved?.Get(player).SovereignBladeBaseBonus ?? 0;

    public static void AddBladeBaseBonus(Player player, decimal amount)
    {
        if (_saved == null || amount <= 0 || !HasReward(player, WeaponKind.SovereignBlade, 2))
            return;
        _saved.Modify(player, state => state.SovereignBladeBaseBonus += amount);
        Changed?.Invoke();
    }

    /// <summary>Count successful combats once; losses and reload-only entries earn no progress.</summary>
    public static void RecordVictory(CombatVictoryEvent victory)
    {
        if (_saved == null)
            return;
        var location = RoomLocations.GetValue(victory.Room,
            room => CaptureLocation(victory.RunState, room));
        var isElite = victory.Room.RoomType == RoomType.Elite;
        var isQuickElite = isElite && victory.CombatState?.RoundNumber is >= 1 and <= QuickEliteMaxRound;
        var changed = false;
        foreach (var player in victory.RunState.Players.Where(static player => player.Character is Regent))
        {
            var state = _saved.Get(player);
            if (state.CountedCombatVictories.Contains(location.Key))
                continue;
            _saved.Modify(player, data =>
            {
                if (!data.CountedCombatVictories.Add(location.Key))
                    return;
                data.CombatVictories++;
                if (location.IsUnknown)
                    data.UnknownCombatVictories++;
                if (isElite && data.CountedEliteVictories.Add(location.Key))
                    data.EliteVictories++;
                if (isQuickElite)
                    data.QuickEliteVictories++;
                if (victory.Room.RoomType == RoomType.Boss)
                {
                    if (victory.RunState.CurrentActIndex == 0)
                        data.FirstBossDefeated = true;
                    else if (victory.RunState.CurrentActIndex == 1)
                        data.SecondBossDefeated = true;
                }
            });
            changed = true;
        }
        if (changed)
            Changed?.Invoke();
    }

    private static void RecordRoomEntered(RoomEnteredEvent entry)
    {
        if (entry.Room is CombatRoom or EventRoom)
            RoomLocations.GetValue(entry.Room, room => CaptureLocation(entry.RunState, room));
    }

    private static void RecordRoomExited(RoomExitedEvent exit)
    {
        if (_saved == null || exit.Room is not EventRoom room || room.CanonicalEvent is AncientEventModel ||
            room.CanonicalEvent is MoreWeaponsRegentExtend.Scripts.Events.DepartureArmory)
            return;
        var run = exit.RunManager.DebugOnlyGetState();
        if (run == null)
            return;
        var location = RoomLocations.GetValue(room, r => CaptureLocation(run, r));
        if (!location.IsUnknown)
            return;
        var changed = false;
        foreach (var player in run.Players.Where(static player => player.Character is Regent))
        {
            var finishedEvent = exit.RunManager.EventSynchronizer.Events.FirstOrDefault(e =>
                e.Owner == player && e.Id == room.CanonicalEvent.Id);
            if (finishedEvent?.IsFinished != true || _saved.Get(player).CountedEvents.Contains(location.Key))
                continue;
            _saved.Modify(player, data =>
            {
                if (data.CountedEvents.Add(location.Key))
                    data.EventsCompleted++;
            });
            changed = true;
        }
        if (changed)
            Changed?.Invoke();
    }

    private static RoomLocation CaptureLocation(IRunState run, AbstractRoom room)
    {
        var coord = run.CurrentMapCoord;
        var roomId = room.Id;
        // The history stores the original map icon even when an event enters an
        // internal combat. Looking at RoomType would lose those unknown-node wins.
        var isUnknown = run.CurrentMapPointHistoryEntry?.MapPointType == MapPointType.Unknown;
        // Entered rooms use native locations, including a room ID for room stacks.
        // Unentered console/test rooms have neither. Their actual room object still
        // deduplicates within this process; deterministic ordinals keep distinct
        // victories separate without random IDs or collapsing every null location.
        if (coord == null && roomId == null)
        {
            var ordinal = run.Players.Where(static player => player.Character is Regent)
                .Select(player => room is CombatRoom ? _saved!.Get(player).CombatVictories :
                    _saved!.Get(player).EventsCompleted).DefaultIfEmpty(0).Max() + 1;
            return new RoomLocation(FormattableString.Invariant($"unlocated|{run.CurrentActIndex}|{room.RoomType}|{ordinal}"),
                isUnknown);
        }
        var mapKey = coord is { } map
            ? FormattableString.Invariant($"{map.col},{map.row}") : "none";
        return new RoomLocation(string.Join("|", run.CurrentActIndex.ToString(CultureInfo.InvariantCulture), mapKey,
            roomId?.ToString(CultureInfo.InvariantCulture) ?? "none"), isUnknown);
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
        if (node is < 1 or > LastNode)
            throw new ArgumentOutOfRangeException(nameof(node));
    }
}
