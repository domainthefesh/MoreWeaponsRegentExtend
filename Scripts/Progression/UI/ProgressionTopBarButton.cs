using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.TopBar;

namespace MoreWeaponsRegentExtend.Scripts.Progression.UI;

[RegisterOwnedTopBarButton("weapon_progression",
    IconPath = "res://MoreWeaponsRegentExtend/scenes/progression/tempering_icon.svg", ButtonOrder = 60)]
public sealed class ProgressionTopBarButton : IModTopBarButtonHandler
{
    private Player? _countPlayer;
    private bool _countDirty = true;
    private int _count = -1;

    public ProgressionTopBarButton() => WeaponProgression.Changed += () => _countDirty = true;

    public bool IsVisible(ModTopBarButtonContext context) => context.Player?.Character is Regent;
    public bool IsOpen(ModTopBarButtonContext context) => NCapstoneContainer.Instance?.CurrentCapstoneScreen is WeaponProgressionScreen;

    public int GetCount(ModTopBarButtonContext context)
    {
        if (context.Player is not { } player)
            return -1;
        if (_countDirty || !ReferenceEquals(_countPlayer, player))
        {
            _countPlayer = player;
            _countDirty = false;
            _count = Enum.GetValues<WeaponKind>().Sum(weapon => Enumerable.Range(1, 3)
                .Count(node => WeaponProgression.GetNodeStatus(player, weapon, node) == WeaponTaskStatus.Claimable));
        }
        return _count == 0 ? -1 : _count;
    }

    public void OnClick(ModTopBarButtonContext context)
    {
        if (context.Player is not { } player)
            return;
        if (IsOpen(context))
        {
            context.CloseCapstoneScreen();
            return;
        }
        var scene = GD.Load<PackedScene>(WeaponProgressionScreen.ScenePath);
        var screen = scene.Instantiate<WeaponProgressionScreen>();
        screen.Configure(ProgressionUiData.ForPlayer(player));
        context.OpenCapstoneScreen(screen);
    }
}
