using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib.Networking.Sidecar;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

/// <summary>Only the weapon and node travel; ownership comes from the transport's sender ID.</summary>
public static class WeaponClaimNetwork
{
    public readonly record struct ClaimRequest(WeaponKind Weapon, int Node);

    private static readonly RitsuLibSidecarSyncMessageDescriptor<ClaimRequest> Descriptor = new(
        ModuleId: Entry.ModId,
        MessageKey: "weapon_reward_claim_v1",
        Serialize: Serialize,
        Deserialize: Deserialize,
        Handle: ReceiveClaim,
        LocationTargeted: true,
        ShouldBuffer: true)
    {
        Mode = NetTransferMode.Reliable,
        FailurePolicy = RitsuLibSidecarSyncFailurePolicy.Required,
        BroadcastScope = RitsuLibSidecarSyncBroadcastScope.ReadyPeers,
        DispatchLocalOnBroadcast = true,
        ShouldBroadcast = true
    };

    public static void Register() => RitsuLibSidecarSyncMessages.Register(Descriptor);

    public static bool RequestClaim(Player player, WeaponKind weapon, int node)
    {
        if (!Enum.IsDefined(weapon) || node is not (1 or 2 or 3) ||
            CombatManager.Instance.IsInProgress ||
            WeaponProgression.GetNodeStatus(player, weapon, node) != WeaponTaskStatus.Claimable)
            return false;

        if (TestMode.IsOn || player.RunState.Players.Count == 1)
        {
            var claimed = WeaponProgression.TryClaim(player, weapon, node);
            if (claimed)
                _ = SaveClaim(player.RunState);
            return claimed;
        }

        var manager = RunManager.Instance;
        if (!LocalContext.IsMe(player) || !ReferenceEquals(manager.DebugOnlyGetState(), player.RunState) ||
            manager.NetService == null)
            return false;

        return RitsuLibSidecarSyncMessages.SendToHostAndBroadcast(manager, Descriptor, new ClaimRequest(weapon, node));
    }

    public static byte[] Serialize(ClaimRequest request)
    {
        if (!Enum.IsDefined(request.Weapon) || request.Node is not (1 or 2 or 3))
            throw new InvalidDataException("Invalid weapon reward claim.");
        return [(byte)request.Weapon, (byte)request.Node];
    }

    public static ClaimRequest Deserialize(ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 2)
            throw new InvalidDataException("Invalid weapon reward claim length.");
        var request = new ClaimRequest((WeaponKind)payload[0], payload[1]);
        if (!Enum.IsDefined(request.Weapon) || request.Node is not (1 or 2 or 3))
            throw new InvalidDataException("Invalid weapon reward claim.");
        return request;
    }

    private static async Task ReceiveClaim(RitsuLibSidecarSyncMessageContext<ClaimRequest> context)
    {
        var run = RunManager.Instance.DebugOnlyGetState();
        if (run == null)
            return;

        if (ApplyClaimFromSender(run, context.SenderNetId, context.Message))
            await SaveClaim(run);
    }

    /// <summary>The receiving path resolves the transport identity, never a payload-supplied owner.</summary>
    public static bool ApplyClaimFromSender(IRunState run, ulong senderNetId, ClaimRequest request)
    {
        if (!Enum.IsDefined(request.Weapon) || request.Node is not (1 or 2 or 3))
            return false;
        var player = run.Players.FirstOrDefault(player => player.NetId == senderNetId);
        return player != null && WeaponProgression.TryClaim(player, request.Weapon, request.Node);
    }

    private static async Task SaveClaim(IRunState run)
    {
        // Claims are run data. Never write profile progress or save an in-progress combat snapshot.
        if (TestMode.IsOn || !ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), run) ||
            RunManager.Instance.NetService?.Type is not (NetGameType.Singleplayer or NetGameType.Host) ||
            (run.CurrentRoom is MegaCrit.Sts2.Core.Rooms.CombatRoom && !CombatManager.Instance.IsOverOrEnding))
            return;

        var room = run.CurrentRoom;
        await SaveManager.Instance.SaveRun(room?.IsPreFinished == true ? room : null, saveProgress: false);
    }
}
