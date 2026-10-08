using System;
using Godot;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

// 复用原版君王之剑的曲线、速度和前后遮挡区间，只处理战斗图层和显隐。
public sealed class WeaponOrbitPresentation
{
    public const string CurvePath = "res://MoreWeaponsRegentExtend/scenes/orbit/SovereignWeaponOrbit.tres";
    public const float VisualScaleMultiplier = 1.35f;
    private const float OrbitSpeed = 60f;
    // scenes/vfx/sovereign_blade.tscn 的 Path 变换，旧版武器场景使用相同数据。
    private static readonly Vector2 PathPosition = new(87.7143f, 79.5227f);
    private static readonly Vector2 PathScale = new(2.14732f, 0.998972f);
    private static Curve2D? _sharedCurve;
    private readonly Node2D _root;
    private readonly Vector2 _restPosition;
    private readonly Curve2D _curve;
    private readonly float _curveLength;
    private bool? _isBehind;

    public WeaponOrbitPresentation(Node2D root, Node2D visual)
    {
        _root = root;
        _restPosition = visual.Position;
        _curve = _sharedCurve ??= ResourceLoader.Load<Curve2D>(CurvePath)
            ?? throw new InvalidOperationException($"Missing weapon orbit curve: {CurvePath}");
        _curveLength = _curve.GetBakedLength();
        if (_curveLength <= 0f)
            throw new InvalidOperationException("Weapon orbit curve has no length.");
        // 只放大一次整个 Visual；贴图、粒子、命中框同步放大，轨道不被缩放。
        visual.Scale *= VisualScaleMultiplier;
        // 继承战斗场景的图层，不用正 ZIndex 越过地图/全局 UI。
        _root.TopLevel = false;
        _root.ZAsRelative = true;
        _root.ZIndex = 0;
    }

    public double Advance(double progress, double delta, bool paused) =>
        paused ? progress : progress + OrbitSpeed * Math.Max(0d, delta) / _curveLength;

    public bool Update(CanvasItem owner, double progress, bool hidden)
    {
        if (!GodotObject.IsInstanceValid(_root) || !GodotObject.IsInstanceValid(owner))
            return false;
        if (hidden)
        {
            _root.Hide();
            return false;
        }

        _root.Show();
        double normalizedProgress = (progress % 1d + 1d) % 1d;
        Vector2 curvePoint = _curve.SampleBakedWithRotation((float)normalizedProgress * _curveLength).Origin;
        // Root 是人物的子节点；因此直接使用原场景局部轨道即可随人物移动/缩放。
        _root.Position = PathPosition + PathScale * curvePoint - _restPosition;

        // NSovereignBladeVfx._Process 的遮挡区间。
        bool behind = normalizedProgress > 0.25d && normalizedProgress < 0.78d;
        if (_isBehind != behind && _root.GetParent() == owner)
        {
            _isBehind = behind;
            // Ready 时父节点可能还在添加子节点，延迟排序也避免打断树遍历。
            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(_root) && !_root.IsQueuedForDeletion()
                    && GodotObject.IsInstanceValid(owner) && _root.GetParent() == owner)
                    owner.MoveChild(_root, behind ? 0 : owner.GetChildCount() - 1);
            }).CallDeferred();
        }
        return true;
    }
}
