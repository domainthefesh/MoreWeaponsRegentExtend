using System;
using Godot;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

// 只控制场景变换，不依赖战斗模型，可用原生 Godot 验证重放和中途打断。
public sealed class WeaponMotion
{
    private readonly Node2D _root;
    private readonly Node2D _visual;
    private readonly Vector2 _restPosition;
    private readonly float _restRotation;
    private readonly Action _onFinished;
    private Tween? _attackTween;

    public Vector2 RestPosition => _restPosition;
    public bool IsAttacking { get; private set; }

    public WeaponMotion(Node2D root, Node2D visual, Action onFinished)
    {
        _root = root;
        _visual = visual;
        _restPosition = visual.Position;
        _restRotation = visual.Rotation;
        _onFinished = onFinished;
    }

    public void Attack(Vector2 targetGlobalPosition)
    {
        if (!GodotObject.IsInstanceValid(_root) || !_root.IsInsideTree()
            || !GodotObject.IsInstanceValid(_visual))
            return;

        // 重放/多段攻击可能打断返回段，先复位再开始，避免把敌人位置当新原点。
        Reset();
        IsAttacking = true;
        Vector2 windup = _visual.GlobalPosition + Vector2.Left * 35f;
        float targetRotation = _visual.GetAngleTo(targetGlobalPosition) + _restRotation;
        _attackTween = _root.CreateTween();
        _attackTween.TweenProperty(_visual, "rotation", targetRotation, 0.06);
        _attackTween.Parallel().TweenProperty(_visual, "global_position", windup, 0.08)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        _attackTween.TweenProperty(_visual, "global_position", targetGlobalPosition, 0.08)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Expo);
        _attackTween.TweenInterval(0.10);
        // 返回用局部坐标；父节点持续环绕/人物移动不会让返回点留在旧的世界坐标。
        _attackTween.TweenProperty(_visual, "position", _restPosition, 0.22)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        _attackTween.Parallel().TweenProperty(_visual, "rotation", _restRotation, 0.22);
        _attackTween.TweenCallback(Callable.From(() =>
        {
            Reset();
            _onFinished();
        }));
    }

    public void Reset()
    {
        _attackTween?.Kill();
        _attackTween = null;
        IsAttacking = false;
        if (!GodotObject.IsInstanceValid(_visual))
            return;
        _visual.Position = _restPosition;
        _visual.Rotation = _restRotation;
    }
}
