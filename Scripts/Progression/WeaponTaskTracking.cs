using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace MoreWeaponsRegentExtend.Scripts.Progression;

/// <summary>Count actual upgrades, not the internal upgrades used by previews and save loading.</summary>
public sealed class WeaponTaskUpgradePatch : IPatchMethod
{
    public static string PatchId => "more_weapons_task_upgrade_count";
    public static string Description => "Count successful real card upgrades toward Tempering";
    public static bool IsCritical => true;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardCmd), nameof(CardCmd.Upgrade), [typeof(IEnumerable<CardModel>), typeof(CardPreviewStyle)])];

    public readonly record struct UpgradeSnapshot(CardModel Card, int UpgradeLevel);

    public static void Prefix(ref IEnumerable<CardModel> cards, out UpgradeSnapshot[] __state)
    {
        // Materialize once so lazy card sources retain the same membership in the
        // command and the post-command comparison; the game's own loop is untouched.
        var actualCards = cards.ToArray();
        cards = actualCards;
        __state = actualCards.Distinct().Select(card => new UpgradeSnapshot(card, card.CurrentUpgradeLevel)).ToArray();
    }

    public static void Postfix(UpgradeSnapshot[] __state)
    {
        foreach (var before in __state)
            WeaponProgression.RecordCardUpgrade(before.Card, before.Card.CurrentUpgradeLevel - before.UpgradeLevel);
    }
}
