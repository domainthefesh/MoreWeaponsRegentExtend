using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using System.Globalization;
using System.Text;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

/// <summary>The game discovers console subclasses from registered mod assemblies.</summary>
public sealed class WeaponTaskConsoleCommand : AbstractConsoleCmd
{
    public override string CmdName => "mwquest";
    public override string Args => "status [weapon] | progress <task> <value> | claim <1..9> [weapon|all]";
    public override string Description => "Inspect weapon quests; singleplayer-only progress and claim commands.";
    public override bool IsNetworked => false;
    public override bool DebugOnly => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer is not { Character: Regent } player)
            return new CmdResult(false, "A Regent run must be in progress.");
        if (args.Length == 0 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            return new CmdResult(true, HelpText);
        var action = args[0].ToLowerInvariant();
        if (action == "status")
        {
            if (!TryGetWeapon(player, args.Length > 1 ? args[1] : null, out var weapon))
                return new CmdResult(false, "For a random-weapon run, specify a WeaponKind (see help mwquest).");
            return new CmdResult(true, StatusText(player, weapon));
        }
        if (player.RunState.Players.Count != 1 ||
            (!TestMode.IsOn && RunManager.Instance.NetService?.Type != NetGameType.Singleplayer))
            return new CmdResult(false, "Quest test mutations are disabled in multiplayer.");
        if (CombatManager.Instance.IsInProgress)
            return new CmdResult(false, "Finish the current combat before editing or claiming quests.");

        if (action == "progress" && args.Length == 3 && TryParseTask(args[1], out var node, out var alternative) &&
            int.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value <= 100000)
        {
            WeaponProgression.SetDebugProgress(player, node, value, alternative);
            return new CmdResult(WeaponClaimNetwork.SaveProgress(player.RunState), true,
                $"Task {args[1]} progress set to {value}. Open the quest screen to claim normally.");
        }
        if (action == "claim" && args.Length is 2 or 3 &&
            int.TryParse(args[1], out node) && node is >= 1 and <= WeaponProgression.LastNode)
        {
            if (args.Length == 3 && args[2].Equals("all", StringComparison.OrdinalIgnoreCase))
                return new CmdResult(ClaimAll(player, node), true, "Claiming this node for all eligible weapons.");
            if (!TryGetWeapon(player, args.Length == 3 ? args[2] : null, out var weapon))
                return new CmdResult(false, "Specify a valid WeaponKind for a random-weapon run.");
            if (WeaponProgression.GetNodeStatus(player, weapon, node) != WeaponTaskStatus.Claimable)
                return new CmdResult(false, $"Cannot claim node {node}: {WeaponProgression.GetNodeStatus(player, weapon, node)}.");
            return new CmdResult(ClaimOne(player, weapon, node), true, $"Claiming {weapon}, node {node}.");
        }
        return new CmdResult(false, HelpText);
    }

    private static async Task ClaimOne(Player player, WeaponKind weapon, int node)
    {
        if (await WeaponProgression.TryClaimAsync(player, weapon, node))
            await WeaponClaimNetwork.SaveProgress(player.RunState);
    }

    private static async Task ClaimAll(Player player, int node)
    {
        foreach (var weapon in Enum.GetValues<WeaponKind>())
            if (WeaponProgression.GetNodeStatus(player, weapon, node) == WeaponTaskStatus.Claimable)
                await WeaponProgression.TryClaimAsync(player, weapon, node);
        await WeaponClaimNetwork.SaveProgress(player.RunState);
    }

    private static bool TryGetWeapon(Player player, string? text, out WeaponKind weapon)
    {
        if (text != null)
            return Enum.TryParse(text, true, out weapon) && Enum.IsDefined(weapon);
        var selected = WeaponSelection.GetWeapon(player);
        weapon = selected ?? default;
        return selected.HasValue;
    }

    private static bool TryParseTask(string task, out int node, out bool alternative)
    {
        alternative = task.Equals("battles", StringComparison.OrdinalIgnoreCase);
        node = task.ToLowerInvariant() switch
        {
            "boss1" => 1, "boss2" => 2, "jade" => 3, "fire" => 4,
            "heavy" => 5, "light" => 6, "water" => 7, "qi" => 8,
            "soul" or "battles" => 9,
            _ => int.TryParse(task, out var number) ? number : 0
        };
        return node is >= 1 and <= WeaponProgression.LastNode;
    }

    private static string StatusText(Player player, WeaponKind weapon)
    {
        var text = new StringBuilder().AppendLine($"Weapon: {weapon}");
        for (var node = 1; node <= WeaponProgression.LastNode; node++)
        {
            text.Append($"{node}: {WeaponProgression.GetNodeProgressText(player, node)}");
            var alt = WeaponProgression.GetNodeAlternativeProgress(player, node);
            if (alt.Required > 0)
                text.Append($" | alternative {alt.Current}/{alt.Required}");
            text.AppendLine($" [{WeaponProgression.GetNodeStatus(player, weapon, node)}]");
        }
        return text.ToString();
    }

    private const string HelpText = "mwquest status [WeaponKind]\n" +
        "mwquest progress <1..9|boss1|boss2|jade|fire|heavy|light|water|qi|soul|battles> <0..100000>\n" +
        "mwquest claim <1..9> [WeaponKind|all]\n" +
        "Tasks: 1/2 bosses; 3 elite wins (5); 4 upgrades (5); 5/6 Act 1 passed; " +
        "7 elite wins within 4 rounds (3); 8 completed events (5); " +
        "9 unknown-node wins (3), or battles (18).\n" +
        "Mutations are singleplayer-only and blocked during combat; claim never bypasses task conditions.\n" +
        "WeaponKind examples: SovereignBludgeon, NeptuneTrident, SovereignDagger, SovereignBlade. " +
        "For random mode, use an explicit WeaponKind or claim all.";
}
