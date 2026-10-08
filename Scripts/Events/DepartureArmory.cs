using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Events;

// Registered for model lookup and save restoration, but never drawn from random event pools.
[RegisterSharedEvent]
public sealed class DepartureArmory : ModEventTemplate
{
    public const string ImageRoot = "res://MoreWeaponsRegentExtend/images/events/DepartureArmory";

    private sealed record WeaponGroup(string Key, string Image, IReadOnlyList<WeaponKind> Weapons);
    private static readonly WeaponGroup[] Groups =
    [
        new("LIGHT", "Light", [WeaponKind.SovereignCrystalBlade, WeaponKind.SovereignDagger,
            WeaponKind.NeptuneTrident, WeaponKind.SovereignKatana, WeaponKind.SeaCalmingStaff]),
        new("HEAVY", "Heavy", [WeaponKind.SovereignScythe, WeaponKind.SovereignAxe, WeaponKind.SovereignBludgeon,
            WeaponKind.SovereignSpear, WeaponKind.SovereignShield]),
        new("RANGED", "Ranged", [WeaponKind.ApocalypseLongbow, WeaponKind.SovereignGun, WeaponKind.GalaxyTrajectoryCannon]),
        new("SPECIAL", "Special", [WeaponKind.SovereignWings, WeaponKind.HolyCodex, WeaponKind.RoyalBrassKnuckles])
    ];

    public override EventAssetProfile AssetProfile => new(InitialPortraitPath: ImageRoot + "Initial.png");
    public override bool IsShared => false;
    public override bool IsAllowed(IRunState runState) => false;

    public override IEnumerable<string> GetAssetPaths(IRunState runState) =>
        base.GetAssetPaths(runState).Concat(Groups.Select(group => ImageRoot + group.Image + ".png"));

    protected override void SetInitialEventState(bool isPreFinished)
    {
        if (Owner!.Character is not Regent || WeaponSelection.IsChosen(Owner))
        {
            SetEventFinished(Owner.Character is Regent ? CompletionDescription() : PageDescription("NOT_REGENT"));
            return;
        }
        // A saved armory may be completed for one teammate while another still needs to choose.
        SetEventState(InitialDescription, GenerateInitialOptions());
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new(this, () => ShowCategories(0), InitialOptionKey("CHOOSE_WEAPON")),
        new(this, () => ChooseWeapon(WeaponKind.SovereignBlade), InitialOptionKey("FAMILIAR")),
        new(this, ChooseRandomOnce, InitialOptionKey("RANDOM_ONCE")),
        new(this, ChooseEveryForge, InitialOptionKey("EVERY_FORGE"))
    ];

    private Task ShowCategories(int pageIndex)
    {
        var page = ArmoryPagination.GetPage(Groups.Length, pageIndex);
        var options = Groups.Skip(page.Start).Take(page.Count)
            .Select(group => new EventOption(this, () => ShowWeapons(group, pageIndex, 0),
                ModOptionKey("CATEGORIES", group.Key))).ToList();
        AddPageOption(options, page, () => ShowCategories((pageIndex + 1) % page.PageCount));
        options.Add(NavigationOption("BACK", GoBack));
        SetPage("CATEGORIES", "Initial", page, options);
        return Task.CompletedTask;
    }

    private Task ShowWeapons(WeaponGroup group, int categoryPage, int pageIndex)
    {
        var page = ArmoryPagination.GetPage(group.Weapons.Count, pageIndex);
        var options = group.Weapons.Skip(page.Start).Take(page.Count)
            .Select(weapon => new EventOption(this, () => ChooseWeapon(weapon),
                ModOptionKey(group.Key, weapon.ToString().ToUpperInvariant()))).ToList();
        AddPageOption(options, page, () => ShowWeapons(group, categoryPage, (pageIndex + 1) % page.PageCount));
        options.Add(NavigationOption("BACK", () => ShowCategories(categoryPage)));
        SetPage(group.Key, group.Image, page, options);
        return Task.CompletedTask;
    }

    private EventOption NavigationOption(string key, Func<Task> action) =>
        new EventOption(this, action, ModOptionKey("NAVIGATION", key)).ThatWontSaveToChoiceHistory();

    private void AddPageOption(List<EventOption> options, ArmoryPage page, Func<Task> nextPage)
    {
        if (page.PageCount > 1)
            options.Add(NavigationOption(page.Index + 1 < page.PageCount ? "NEXT" : "FIRST", nextPage));
    }

    private void SetPage(string key, string image, ArmoryPage page, IReadOnlyList<EventOption> options)
    {
        if (options.Count > 4)
            throw new InvalidOperationException("Armory pages must have at most four options.");
        var description = PageDescription(key);
        description.Add("Page", page.Index + 1);
        description.Add("Pages", page.PageCount);
        SetEventState(description, options);
        SetPortrait(image);
    }

    private Task GoBack()
    {
        SetEventState(InitialDescription, GenerateInitialOptions());
        SetPortrait("Initial");
        return Task.CompletedTask;
    }

    private Task ChooseWeapon(WeaponKind weapon)
    {
        WeaponSelection.Select(Owner!, weapon);
        return FinishSelection();
    }

    private Task ChooseRandomOnce()
    {
        WeaponSelection.SelectRandomOnce(Owner!);
        return FinishSelection();
    }

    private Task ChooseEveryForge()
    {
        WeaponSelection.SelectEveryForge(Owner!);
        return FinishSelection();
    }

    private async Task FinishSelection()
    {
        SetEventFinished(CompletionDescription());

        // Save this specific room so reloading never reapplies Neow's reward.
        if (Owner!.RunState.CurrentRoom is EventRoom room && room.CanonicalEvent is DepartureArmory)
        {
            if (RunManager.Instance.EventSynchronizer.Events.All(e => e.IsFinished))
                room.MarkPreFinished();
            await SaveManager.Instance.SaveRun(room);
        }
    }

    private MegaCrit.Sts2.Core.Localization.LocString CompletionDescription()
    {
        var description = PageDescription(WeaponSelection.IsEveryForgeRandom(Owner!) ? "EVERY_FORGE_DONE" : "DONE");
        if (WeaponSelection.GetWeapon(Owner!) is { } weapon)
            description.Add("Weapon", L10NLookup($"{Id.Entry}.weapons.{weapon.ToString().ToUpperInvariant()}"));
        return description;
    }

    private void SetPortrait(string image)
    {
        // Every peer has a mutable event for each player. Only update the local player's picture.
        if (LocalContext.IsMe(Owner!) && NEventRoom.Instance?.Layout != null)
            NEventRoom.Instance.SetPortrait(PreloadManager.Cache.GetTexture2D(ImageRoot + image + ".png"));
    }
}
