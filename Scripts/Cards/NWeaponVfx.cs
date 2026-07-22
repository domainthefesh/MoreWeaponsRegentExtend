using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
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
    private static readonly Dictionary<CardModel, NWeaponVfx> ActiveVfx = new();

    public CardModel Card { get; }
    public Node2D Root { get; }

    private Node2D? _spineNode;
    private MegaSprite? _animController;
    private Control? _hitbox;
    private NSelectionReticle? _reticle;
    private NHoverTipSet? _hoverTip;
    private Tween? _attackTween;
    private bool _isFocused;
    private bool _isAttacking;

    public static NWeaponVfx Create(CardModel card, string sceneName)
    {
        var originalCard = GetOriginalCard(card);
        RemoveFor(originalCard);

        var packed = ResourceLoader.Load<PackedScene>(BasePath + sceneName);
        var root = packed.Instantiate<Node2D>();
        var vfx = new NWeaponVfx(originalCard, root);
        ActiveVfx[originalCard] = vfx;
        return vfx;
    }

    public static NWeaponVfx EnsureAttached(CardModel card, string sceneName)
    {
        var originalCard = GetOriginalCard(card);
        if (!ActiveVfx.TryGetValue(originalCard, out var vfx))
            vfx = Create(originalCard, sceneName);

        if (vfx.Root.GetParent() == null)
        {
            Player? owner = originalCard.Owner ?? card.Owner;
            var creatureNode = owner == null ? null : NCombatRoom.Instance?.GetCreatureNode(owner.Creature);
            creatureNode?.AddChildSafely(vfx.Root);
            vfx.Root.Position = Vector2.Zero;
        }

        return vfx;
    }

    public static void AttackFor(CardModel card, Creature? target)
    {
        if (target == null)
            return;

        var originalCard = GetOriginalCard(card);
        if (!ActiveVfx.TryGetValue(originalCard, out var vfx))
            return;

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (creatureNode != null)
            vfx.Attack(creatureNode.VfxSpawnPosition);
    }

    public static void RemoveFor(CardModel card)
    {
        var originalCard = GetOriginalCard(card);
        if (!ActiveVfx.Remove(originalCard, out var vfx))
            return;

        vfx.RemoveWeapon(removeFromRegistry: false);
    }

    private static CardModel GetOriginalCard(CardModel card) => card.DupeOf ?? card;

    private static void RunIfValid(Node? node, System.Action action)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
            return;

        action();
    }

    private NWeaponVfx(CardModel card, Node2D root)
    {
        Card = card;
        Root = root;

        _spineNode = root.GetNodeOrNull<Node2D>("SpineSword");
        if (_spineNode != null)
        {
            _animController = new MegaSprite(_spineNode);
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
    public void Attack(Vector2 targetPos)
    {
        if (_spineNode == null || !GodotObject.IsInstanceValid(_spineNode))
            return;

        if (_isAttacking)
            CleanupAttack();

        _isAttacking = true;
        HideHoverTip();
        _animController?.GetAnimationState().SetAnimation("attack", loop: false);

        var startPos = _spineNode.GlobalPosition;
        var windupPos = startPos + Vector2.Left * 50f;
        _attackTween = Root.CreateTween();
        _attackTween.TweenProperty(_spineNode, "rotation", _spineNode.GetAngleTo(targetPos), 0.05);
        _attackTween.Parallel().TweenProperty(_spineNode, "global_position", windupPos, 0.08)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _attackTween.Chain().TweenProperty(_spineNode, "global_position", targetPos, 0.05)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Expo);
        _attackTween.Chain().TweenInterval(0.15);
        _attackTween.Chain().TweenProperty(_spineNode, "global_position", startPos, 0.18)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        _attackTween.Parallel().TweenProperty(_spineNode, "rotation", 0f, 0.18);
        _attackTween.Chain().TweenCallback(Callable.From(CleanupAttack));
    }

    public void RemoveWeapon() => RemoveWeapon(removeFromRegistry: true);

    private void RemoveWeapon(bool removeFromRegistry)
    {
        HideHoverTip();
        if (_hitbox != null)
        {
            _hitbox.MouseEntered -= OnFocused;
            _hitbox.MouseExited -= OnUnfocused;
            if (NTargetManager.Instance != null)
            {
                NTargetManager.Instance.TargetingBegan -= OnTargetingBegan;
                NTargetManager.Instance.TargetingEnded -= OnTargetingEnded;
            }
        }
        _attackTween?.Kill();
        if (removeFromRegistry)
            ActiveVfx.Remove(GetOriginalCard(Card));
        RunIfValid(Root, Root.QueueFree);
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

    private void CleanupAttack()
    {
        _isAttacking = false;
        _attackTween?.Kill();
        _attackTween = null;
        _animController?.GetAnimationState().SetAnimation("idle_loop");
        if (_spineNode != null && GodotObject.IsInstanceValid(_spineNode))
            _spineNode.Rotation = 0f;
        UpdateHoverTip();
    }
}
