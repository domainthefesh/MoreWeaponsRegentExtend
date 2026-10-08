using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;

namespace MoreWeaponsRegentExtend.Scripts.Progression.UI;

/// <summary>The screen depends on state queries, so its real layout can also be tested outside a run.</summary>
public sealed class ProgressionUiData
{
    public required Func<WeaponKind, int, WeaponTaskStatus> GetStatus { get; init; }
    public required Func<WeaponKind, bool> IsEligible { get; init; }
    public required Func<WeaponKind, int, bool> Claim { get; init; }
    public WeaponKind? SelectedWeapon { get; init; }
    public bool EveryForgeRandom { get; init; }
    public Func<bool> CanClaim { get; init; } = static () => true;
    public Func<int> GetEliteVictories { get; init; } = static () => 0;

    public static ProgressionUiData ForPlayer(Player player) => new()
    {
        GetStatus = (weapon, node) => WeaponProgression.GetNodeStatus(player, weapon, node),
        IsEligible = weapon => WeaponProgression.IsEligible(player, weapon),
        Claim = (weapon, node) => WeaponProgression.RequestClaim(player, weapon, node),
        SelectedWeapon = WeaponSelection.GetWeapon(player),
        EveryForgeRandom = WeaponSelection.IsEveryForgeRandom(player),
        CanClaim = static () => !CombatManager.Instance.IsInProgress,
        GetEliteVictories = () => WeaponProgression.GetEliteVictories(player)
    };
}
