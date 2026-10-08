using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

// 统一武器原生场景的头顶环绕、攻击复位和生命周期，保留旧 Spine 兼容。
public sealed class NWeaponVfx
{
    public const string BasePath = "res://MoreWeaponsRegentExtend/scenes/";
    private static readonly Dictionary<CardModel, NWeaponVfx> ActiveVfx = new();
    private static CombatManager? _cleanupManager;
    private static NMapScreen? _observedMap;
    private static readonly Dictionary<NSovereignBladeVfx, bool> HiddenLegacyVfx = new();

    public CardModel Card { get; }
    public Node2D Root { get; }
    public double OrbitProgress { get; set; }

    private readonly MegaSprite? _animController;
    private readonly Control? _hitbox;
    private readonly NSelectionReticle? _reticle;
    private readonly WeaponMotion _motion;
    private readonly WeaponOrbitPresentation _presentation;
    private NCreature? _ownerNode;
    private SceneTree? _tree;
    private NTargetManager? _targetManager;
    private NHoverTipSet? _hoverTip;
    private bool _isFocused;
    private bool _removed;

    public static NWeaponVfx? EnsureAttached(CardModel card, string sceneName)
    {
        if (TestMode.IsOn || (!CombatManager.Instance.IsStarting && CombatManager.Instance.IsOverOrEnding))
            return null;
        var originalCard = GetOriginalCard(card);
        Player? owner = originalCard.Owner ?? card.Owner;
        var creatureNode = owner == null ? null : NCombatRoom.Instance?.GetCreatureNode(owner.Creature);
        if (creatureNode == null || !GodotObject.IsInstanceValid(creatureNode) || !creatureNode.IsInsideTree())
            return null;

        ObserveCombatUi();
        Node2D? root = null;
        try
        {
            if (ActiveVfx.TryGetValue(originalCard, out var existing)
                && !existing._removed && GodotObject.IsInstanceValid(existing.Root)
                && existing._ownerNode == creatureNode)
                return existing;

            if (card.IsDupe)
                return null;
            RemoveFor(originalCard);
            var packed = ResourceLoader.Load<PackedScene>(BasePath + sceneName);
            if (packed == null)
                return null;
            root = packed.Instantiate<Node2D>();
            var vfx = new NWeaponVfx(originalCard, root) { _ownerNode = creatureNode };
            ActiveVfx[originalCard] = vfx;
            RebalanceOrbit(owner);
            vfx.UpdateOrbit();
            creatureNode.AddChildSafely(root);
            return vfx;
        }
        catch (Exception exception)
        {
            RemoveFor(originalCard);
            if (root != null && GodotObject.IsInstanceValid(root) && !root.IsQueuedForDeletion())
                root.QueueFree();
            GD.PushWarning($"MoreWeaponsRegentExtend: orbit VFX could not attach: {exception.Message}");
            return null;
        }
    }

    public static void AttackFor(CardModel card, Creature? target)
    {
        if (target == null)
            return;
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (creatureNode != null && GodotObject.IsInstanceValid(creatureNode)
            && ActiveVfx.TryGetValue(GetOriginalCard(card), out var vfx))
        {
            try
            {
                vfx.Attack(creatureNode.VfxSpawnPosition);
            }
            catch (Exception exception)
            {
                vfx.RemoveWeapon();
                GD.PushWarning($"MoreWeaponsRegentExtend: orbit attack VFX could not play: {exception.Message}");
            }
        }
    }

    public static void RemoveFor(CardModel card)
    {
        // 自动重放的临时复制牌共享攻击动画，离场时不能移除原牌的武器。
        if (card.IsDupe)
            return;
        if (ActiveVfx.TryGetValue(GetOriginalCard(card), out var vfx))
            vfx.RemoveWeapon();
    }

    public static void ObserveCombatUi()
    {
        if (TestMode.IsOn)
            return;
        var manager = CombatManager.Instance;
        if (!ReferenceEquals(_cleanupManager, manager))
        {
            if (_cleanupManager != null)
                _cleanupManager.CombatEnded -= OnCombatEnded;
            _cleanupManager = manager;
            _cleanupManager.CombatEnded += OnCombatEnded;
        }

        var map = NMapScreen.Instance;
        if (map != null && GodotObject.IsInstanceValid(map) && _observedMap != map)
        {
            if (_observedMap != null && GodotObject.IsInstanceValid(_observedMap))
            {
                _observedMap.Opened -= OnMapOpened;
                _observedMap.Closed -= OnMapClosed;
            }
            HiddenLegacyVfx.Clear();
            _observedMap = map;
            _observedMap.Opened += OnMapOpened;
            _observedMap.Closed += OnMapClosed;
        }
        if (map?.IsOpen == true)
        {
            OnMapOpened();
            // 铸造场景可能通过 AddChildSafely 延迟挂载，补一次同帧尾部隐藏。
            Callable.From(() =>
            {
                if (NMapScreen.Instance?.IsOpen == true)
                    OnMapOpened();
            }).CallDeferred();
        }
    }

    private static IEnumerable<NSovereignBladeVfx> GetLegacyModWeapons()
    {
        var room = NCombatRoom.Instance;
        return room == null || !GodotObject.IsInstanceValid(room)
            ? Array.Empty<NSovereignBladeVfx>()
            : room.FindChildren("*", "", true, false).OfType<NSovereignBladeVfx>()
                .Where(vfx => GodotObject.IsInstanceValid(vfx) && !vfx.IsQueuedForDeletion()
                    && vfx.Card is MoreWeaponsCardBase).ToArray();
    }

    private static void OnMapOpened()
    {
        foreach (var vfx in ActiveVfx.Values.ToArray())
        {
            if (GodotObject.IsInstanceValid(vfx.Root))
                vfx.Root.Hide();
            vfx._isFocused = false;
            vfx.HideHoverTip();
        }
        // 兼容旧场景遗留的本模组实例，只处理本模组，不动原版君王剑。
        foreach (var vfx in GetLegacyModWeapons())
        {
            HiddenLegacyVfx.TryAdd(vfx, vfx.Visible);
            vfx.Hide();
        }
    }

    private static void OnMapClosed()
    {
        foreach (var vfx in ActiveVfx.Values.ToArray())
            vfx.UpdateOrbit();
        foreach (var pair in HiddenLegacyVfx)
            if (pair.Value && GodotObject.IsInstanceValid(pair.Key) && !pair.Key.IsQueuedForDeletion()
                && !CombatManager.Instance.IsOverOrEnding)
                pair.Key.Show();
        HiddenLegacyVfx.Clear();
    }

    private static void OnCombatEnded(CombatRoom room) => RemoveAll();

    public static void RemoveAll()
    {
        if (TestMode.IsOn)
            return;
        // 胜利/失败时战斗房间还在树中，主动清理，不依赖 TreeExiting。
        foreach (var vfx in ActiveVfx.Values.ToArray())
            vfx.RemoveWeapon();
        foreach (var vfx in GetLegacyModWeapons())
        {
            // 战斗已经结束，无需等待原版缩小动画；直接释放并取消它的绑定 Tween。
            vfx.Hide();
            vfx.QueueFree();
        }
        HiddenLegacyVfx.Clear();
    }

    private static CardModel GetOriginalCard(CardModel card) => card.DupeOf ?? card;

    private static void RebalanceOrbit(Player? owner)
    {
        var weapons = ActiveVfx.Values.Where(vfx => !vfx._removed && vfx.Card.Owner == owner).ToArray();
        for (int i = 0; i < weapons.Length; i++)
            weapons[i].OrbitProgress = (double)i / weapons.Length;
    }

    private NWeaponVfx(CardModel card, Node2D root)
    {
        Card = card;
        Root = root;
        var spineNode = root.GetNodeOrNull<Node2D>("SpineSword");
        var visual = spineNode ?? root.GetNode<Node2D>("Visual");
        if (spineNode != null)
        {
            _animController = new MegaSprite(spineNode);
            _animController.GetAnimationState().SetAnimation("idle_loop");
        }
        _motion = new WeaponMotion(root, visual, OnAttackFinished);
        _presentation = new WeaponOrbitPresentation(root, visual);
        _hitbox = visual.GetNodeOrNull<Control>("Hitbox");
        _reticle = _hitbox?.GetNodeOrNull<Node>("SelectionReticle") as NSelectionReticle;
        Root.Ready += OnReady;
        Root.TreeExiting += RemoveWeapon;
    }

    private void OnReady()
    {
        _tree = Root.GetTree();
        _tree.ProcessFrame += OnProcessFrame;
        _targetManager = NTargetManager.Instance;
        if (_hitbox != null)
        {
            _hitbox.MouseFilter = Control.MouseFilterEnum.Stop;
            _hitbox.MouseEntered += OnFocused;
            _hitbox.MouseExited += OnUnfocused;
            if (_targetManager != null)
            {
                _targetManager.TargetingBegan += OnTargetingBegan;
                _targetManager.TargetingEnded += OnTargetingEnded;
            }
        }
        UpdateOrbit();
    }

    private void OnProcessFrame() => UpdateOrbit(Root.GetProcessDeltaTime());

    private void UpdateOrbit() => UpdateOrbit(0d);

    private void UpdateOrbit(double delta)
    {
        if (_removed || !GodotObject.IsInstanceValid(Root))
            return;
        var combat = CombatManager.Instance;
        if (!combat.IsStarting && combat.IsOverOrEnding)
        {
            RemoveWeapon();
            return;
        }
        if (_ownerNode == null || !GodotObject.IsInstanceValid(_ownerNode))
        {
            RemoveWeapon();
            return;
        }
        bool hidden = NMapScreen.Instance?.IsOpen == true || !_ownerNode.IsVisibleInTree();
        OrbitProgress = _presentation.Advance(OrbitProgress, delta, _hoverTip != null);
        if (!_presentation.Update(_ownerNode, OrbitProgress, hidden))
            HideHoverTip();
    }

    public void Forge(float bladeDamage = 0f, bool showFlames = false) { }

    public void Attack(Vector2 targetPos)
    {
        if (_removed || !GodotObject.IsInstanceValid(Root))
            return;
        HideHoverTip();
        _animController?.GetAnimationState().SetAnimation("attack", loop: false);
        _motion.Attack(targetPos);
    }

    private void OnAttackFinished()
    {
        if (_removed)
            return;
        _animController?.GetAnimationState().SetAnimation("idle_loop");
        UpdateHoverTip();
    }

    public void RemoveWeapon()
    {
        if (_removed)
            return;
        _removed = true;
        if (GodotObject.IsInstanceValid(Root))
            Root.Hide();
        HideHoverTip();
        _motion.Reset();
        if (_tree != null && GodotObject.IsInstanceValid(_tree))
            _tree.ProcessFrame -= OnProcessFrame;
        if (_hitbox != null && GodotObject.IsInstanceValid(_hitbox))
        {
            _hitbox.MouseEntered -= OnFocused;
            _hitbox.MouseExited -= OnUnfocused;
        }
        if (_targetManager != null && GodotObject.IsInstanceValid(_targetManager))
        {
            _targetManager.TargetingBegan -= OnTargetingBegan;
            _targetManager.TargetingEnded -= OnTargetingEnded;
        }
        ActiveVfx.Remove(GetOriginalCard(Card));
        RebalanceOrbit(Card.Owner);
        if (GodotObject.IsInstanceValid(Root) && !Root.IsQueuedForDeletion())
            Root.QueueFree();
    }

    private void OnFocused()
    {
        _isFocused = true;
        if (NCombatRoom.Instance?.Ui?.Hand?.InCardPlay == false)
            UpdateHoverTip();
    }

    private void OnUnfocused() { _isFocused = false; UpdateHoverTip(); }
    private void OnTargetingBegan()
    {
        if (_hitbox != null) _hitbox.MouseFilter = Control.MouseFilterEnum.Ignore;
        UpdateHoverTip();
    }
    private void OnTargetingEnded()
    {
        if (_hitbox != null) _hitbox.MouseFilter = Control.MouseFilterEnum.Stop;
        UpdateHoverTip();
    }

    private void UpdateHoverTip()
    {
        if (_hitbox == null || !GodotObject.IsInstanceValid(_hitbox) || _removed)
            return;
        bool show = _isFocused && Root.IsVisibleInTree() && NMapScreen.Instance?.IsOpen != true
            && !_motion.IsAttacking && _targetManager?.IsInSelection != true
            && _hitbox.MouseFilter != Control.MouseFilterEnum.Ignore;
        if (show)
        {
            _reticle?.OnSelect();
            if (_hoverTip == null)
            {
                _hoverTip = NHoverTipSet.CreateAndShow(_hitbox, HoverTipFactory.FromCard(Card));
                var bounds = _hitbox.GetGlobalRect();
                _hoverTip?.SetGlobalPosition(bounds.Position + Vector2.Right * bounds.Size.X);
            }
        }
        else
            HideHoverTip();
    }

    private void HideHoverTip()
    {
        if (_hitbox == null || !GodotObject.IsInstanceValid(_hitbox))
            return;
        _reticle?.OnDeselect();
        if (_hoverTip != null)
        {
            NHoverTipSet.Remove(_hitbox);
            _hoverTip = null;
        }
    }
}
