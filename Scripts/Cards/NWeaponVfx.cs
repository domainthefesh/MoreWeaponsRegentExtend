using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

// 通用武器 VFX：加载指定 tscn 场景，控制动画和悬停提示
public class NWeaponVfx
{
    public static readonly string BasePath = "res://MoreWeaponsRegentExtend/scenes/";

    public CardModel Card { get; }
    public Node2D Root { get; }

    private MegaSprite? _animController;
    private Control? _hitbox;
    private NSelectionReticle? _reticle;
    private NHoverTipSet? _hoverTip;
    private bool _isFocused;

    public static NWeaponVfx Create(CardModel card, string sceneName)
    {
        var packed = ResourceLoader.Load<PackedScene>(BasePath + sceneName);
        var root = packed.Instantiate<Node2D>();
        return new NWeaponVfx(card, root);
    }

    private NWeaponVfx(CardModel card, Node2D root)
    {
        Card = card;
        Root = root;

        var spineNode = root.GetNodeOrNull<Node2D>("SpineSword");
        if (spineNode != null)
        {
            _animController = new MegaSprite(spineNode);
            _animController.GetAnimationState().SetAnimation("idle_loop");
        }

        _hitbox = root.GetNodeOrNull<Control>("SpineSword/Hitbox");
        if (_hitbox != null)
        {
            _hitbox.MouseFilter = Control.MouseFilterEnum.Stop;
            _reticle = _hitbox.GetNodeOrNull<NSelectionReticle>("SelectionReticle");
            _hitbox.MouseEntered += OnFocused;
            _hitbox.MouseExited += OnUnfocused;
            NTargetManager.Instance.TargetingBegan += OnTargetingBegan;
            NTargetManager.Instance.TargetingEnded += OnTargetingEnded;
        }
    }

    public void Forge(float bladeDamage = 0f, bool showFlames = false) { }
    public void Attack(Vector2 targetPos) { _animController?.GetAnimationState().SetAnimation("attack", loop: false); }

    public void RemoveWeapon()
    {
        HideHoverTip();
        if (_hitbox != null)
        {
            _hitbox.MouseEntered -= OnFocused;
            _hitbox.MouseExited -= OnUnfocused;
            NTargetManager.Instance.TargetingBegan -= OnTargetingBegan;
            NTargetManager.Instance.TargetingEnded -= OnTargetingEnded;
        }
        Root.QueueFree();
    }

    public double OrbitProgress { get; set; }

    private void OnFocused()
    {
        _isFocused = true;
        if (NCombatRoom.Instance?.Ui?.Hand?.InCardPlay == false) UpdateHoverTip();
    }

    private void OnUnfocused() { _isFocused = false; UpdateHoverTip(); }

    private void OnTargetingBegan() { if (_hitbox != null) _hitbox.MouseFilter = Control.MouseFilterEnum.Ignore; UpdateHoverTip(); }
    private void OnTargetingEnded() { if (_hitbox != null) _hitbox.MouseFilter = Control.MouseFilterEnum.Stop; UpdateHoverTip(); }

    private void UpdateHoverTip()
    {
        if (_hitbox == null) return;
        bool show = _isFocused && !NTargetManager.Instance.IsInSelection && _hitbox.MouseFilter != Control.MouseFilterEnum.Ignore;
        if (show)
        {
            _reticle?.OnSelect();
            if (_hoverTip == null)
            {
                _hoverTip = NHoverTipSet.CreateAndShow(_hitbox, HoverTipFactory.FromCard(Card));
                _hoverTip?.SetGlobalPosition(_hitbox.GlobalPosition + Vector2.Right * _hitbox.Size.X);
            }
        }
        else
        {
            _reticle?.OnDeselect();
            if (_hoverTip != null) { NHoverTipSet.Remove(_hitbox); _hoverTip = null; }
        }
    }

    private void HideHoverTip()
    {
        _reticle?.OnDeselect();
        if (_hoverTip != null && _hitbox != null) { NHoverTipSet.Remove(_hitbox); _hoverTip = null; }
    }
}
