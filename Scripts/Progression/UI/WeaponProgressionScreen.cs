using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace MoreWeaponsRegentExtend.Scripts.Progression.UI;

/// <summary>The royal task graph, with an independent side task and details opened on demand.</summary>
public partial class WeaponProgressionScreen : Control, ICapstoneScreen
{
    public const string ScenePath = "res://MoreWeaponsRegentExtend/scenes/progression/weapon_progression.tscn";
    private static readonly Vector2 DesignSize = new(1600, 900);
    private static readonly Color Gold = new("e7c44e"), Paper = new("eee5ce"), Ink = new("17272d");
    private static readonly Color Muted = new("9aafab"), Cyan = new("9bc8c7"), Outline = new("111d22");
    private ProgressionUiData _data = null!;
    private Control _frame = null!;
    private Panel _graph = null!, _detail = null!;
    private ScrollContainer _weaponScroll = null!;
    private QuestPath _path = null!;
    private Label _weaponName = null!, _detailWeapon = null!, _reward = null!, _status = null!, _step = null!, _task = null!;
    private TextureRect _weaponArt = null!;
    private Button _claim = null!, _close = null!, _detailClose = null!;
    private Texture2D? _nativeBar, _nativeSlot;
    private readonly Dictionary<WeaponKind, Button> _weaponButtons = new();
    private readonly Dictionary<WeaponKind, Label> _weaponNames = new();
    private readonly Button[] _nodes = new Button[3];
    private readonly Panel[] _nodePlates = new Panel[3];
    private readonly Label[] _nodeCaptions = new Label[3];
    private readonly Control[] _nodeTicks = new Control[3];
    private readonly TextureRect[] _nodeArts = new TextureRect[2];
    private bool _blocking, _closing, _closed, _dragging;
    private Vector2 _dragOrigin, _panOrigin;
    private float _zoom = 1;
    private int _lastFocusedNode = 1;
    private Node? _hudRoot;
    private Control? _hudTopBarBackground, _hudRelicInventory;
    private Rect2 _lastSafeRect;
    public WeaponKind ViewedWeapon { get; private set; }
    public int ViewedNode { get; private set; }
    public NetScreenType ScreenType => NetScreenType.Compendium;
    public bool UseSharedBackstop => false;
    public Control? DefaultFocusedControl => ViewedNode is >= 1 and <= 3 && _detail?.Visible == true
        ? (_claim.Disabled ? _detailClose : _claim)
        : _nodes.ElementAtOrDefault(Math.Clamp(_lastFocusedNode - 1, 0, 2)) ?? _close;
    public event Action? Closed;

    public void Configure(ProgressionUiData data)
    {
        _data = data;
        ViewedWeapon = data.SelectedWeapon ?? WeaponKind.SovereignBludgeon;
        ViewedNode = 0;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var language = LocManager.Instance?.Language ?? "zhs";
        var font = FontManager.GetSubstituteFont(language, FontType.Regular)
            ?? GD.Load<Font>("res://themes/kreon_regular_shared.tres");
        Theme = new Theme { DefaultFont = font };
        _nativeBar = LoadTexture("res://images/ui/reward_screen/reward_item_button.png");
        _nativeSlot = LoadTexture("res://images/atlases/ui_atlas.sprites/checkbox_unticked.tres");
        Build();
        Resized += Layout;
        WeaponProgression.Changed += Refresh;
        Layout();
        Refresh();
    }

    public override void _ExitTree()
    {
        WeaponProgression.Changed -= Refresh;
        ReleaseInputBlock();
    }

    public override void _Process(double delta)
    {
        if (_frame == null || !IsVisibleInTree()) return;
        // Relics can wrap into another row without resizing the capstone itself.
        var safeRect = GetHudSafeRect();
        if (!safeRect.IsEqualApprox(_lastSafeRect)) Layout(safeRect);
    }

    public override void _Input(InputEvent input)
    {
        if (!IsVisibleInTree()) return;
        if (input is InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Left })
            _dragging = false;
        if ((input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }) ||
            input.IsActionPressed(MegaInput.cancel) || input.IsActionPressed(MegaInput.back) ||
            input.IsActionPressed(MegaInput.pauseAndBack))
        {
            GetViewport().SetInputAsHandled();
            if (ViewedNode != 0) CloseDetails();
            else RequestClose();
        }
    }

    public void AfterCapstoneOpened()
    {
        Visible = true;
        if (!_blocking && NHotkeyManager.Instance is { } hotkeys)
        {
            hotkeys.AddBlockingScreen(this);
            _blocking = true;
        }
        Callable.From(() => DefaultFocusedControl?.GrabFocus()).CallDeferred();
    }

    public void AfterCapstoneClosed()
    {
        if (_closed) return;
        _closed = _closing = true;
        Visible = false;
        ReleaseInputBlock();
        Closed?.Invoke();
        QueueFree();
    }

    private void ReleaseInputBlock()
    {
        if (!_blocking) return;
        NHotkeyManager.Instance?.RemoveBlockingScreen(this);
        _blocking = false;
    }

    public void RequestClose()
    {
        if (_closing) return;
        _closing = true;
        if (NCapstoneContainer.Instance?.CurrentCapstoneScreen == this)
            NCapstoneContainer.Instance.Close();
        else AfterCapstoneClosed();
    }

    public void SelectWeapon(WeaponKind weapon)
    {
        if (!Enum.IsDefined(weapon)) throw new ArgumentOutOfRangeException(nameof(weapon));
        ViewedWeapon = weapon;
        ViewedNode = 0;
        Refresh();
    }

    public void SelectNode(int node)
    {
        if (node is not (1 or 2 or 3)) throw new ArgumentOutOfRangeException(nameof(node));
        ViewedNode = _lastFocusedNode = node;
        Refresh();
        Callable.From(() =>
        {
            if (!_closing && IsInsideTree()) DefaultFocusedControl?.GrabFocus();
        }).CallDeferred();
    }

    public void CloseDetails()
    {
        ViewedNode = 0;
        Refresh();
        _nodes.ElementAtOrDefault(_lastFocusedNode - 1)?.GrabFocus();
    }

    public bool ClaimSelected()
    {
        if (ViewedNode == 0 || !_data.CanClaim() ||
            _data.GetStatus(ViewedWeapon, ViewedNode) != WeaponTaskStatus.Claimable) return false;
        var claimed = _data.Claim(ViewedWeapon, ViewedNode);
        Refresh();
        return claimed;
    }

    private void Build()
    {
        var shade = new ColorRect { Color = new Color(0.03f, 0.08f, 0.11f, 0.97f), MouseFilter = MouseFilterEnum.Stop };
        AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _frame = new Panel { Name = "TemperingFrame", MouseFilter = MouseFilterEnum.Stop };
        _frame.AddThemeStyleboxOverride("panel", Box(new Color("23353a"), new Color(0, 0, 0, 0), 0, 0));
        Place(_frame, this, Vector2.Zero, DesignSize);
        AddLabel(_frame, "Title", ProgressionUiText.Get("TITLE"), new(49, 38), new(1210, 65), 43, Gold, 6);
        AddRule(_frame, new(49, 111), new(1507, 2), new Color("10232a"));
        AddRule(_frame, new(49, 113), new(1507, 1), new Color("4a6866"));
        _close = MakeButton("Close", "×", 31);
        _close.TooltipText = ProgressionUiText.Get("CLOSE");
        Place(_close, _frame, new(1512, 48), new(45, 45));
        _close.Pressed += RequestClose;
        BuildWeaponIndex();
        BuildPath();
        BuildDetail();
        BuildGraphTools();
        ConfigureFocus();
    }

    private void BuildWeaponIndex()
    {
        var heading = new Panel { Name = "ArmoryHeading", MouseFilter = MouseFilterEnum.Ignore };
        heading.AddThemeStyleboxOverride("panel", NativeBox(_nativeBar, new Color(0.56f, 0.65f, 0.62f), 0));
        Place(heading, _frame, new(38, 144), new(238, 60));
        AddLabel(heading, "WeaponsHeading", ProgressionUiText.Get("WEAPONS"), new(18, 4), new(202, 50), 26, Gold);
        _weaponScroll = new ScrollContainer
        {
            Name = "WeaponIndex", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true, MouseFilter = MouseFilterEnum.Stop
        };
        Place(_weaponScroll, _frame, new(39, 223), new(236, 629));
        var items = new VBoxContainer { Name = "Weapons", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        items.AddThemeConstantOverride("separation", 4);
        _weaponScroll.AddChild(items);
        foreach (var weapon in Enum.GetValues<WeaponKind>())
        {
            var button = MakeButton("Weapon_" + weapon, "", 20);
            button.CustomMinimumSize = new Vector2(219, 63);
            button.Alignment = HorizontalAlignment.Left;
            items.AddChild(button);
            var icon = new TextureRect
            {
                Name = "Icon", MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Texture = WeaponTexture(weapon)
            };
            Place(icon, button, new(6, 10), new(53, 43));
            var name = AddLabel(button, "Name", ProgressionUiText.WeaponName(weapon),
                new(65, 0), new(155, 63), 20, Paper);
            name.VerticalAlignment = VerticalAlignment.Center;
            name.ClipText = true;
            name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            name.Size = new Vector2(155, 63);
            button.TooltipText = ProgressionUiText.WeaponName(weapon);
            button.Pressed += () => SelectWeapon(weapon);
            _weaponButtons.Add(weapon, button);
            _weaponNames.Add(weapon, name);
        }
    }

    private void BuildPath()
    {
        _graph = new Panel { Name = "Graph", MouseFilter = MouseFilterEnum.Stop, ClipContents = true };
        _graph.AddThemeStyleboxOverride("panel", Box(new Color("263a3e"), new Color("58746c"), 2, 15));
        Place(_graph, _frame, new(306, 144), new(1256, 710));
        _graph.MouseDefaultCursorShape = CursorShape.Drag;
        _graph.GuiInput += OnGraphInput;
        _weaponName = AddLabel(_graph, "ViewedWeapon", "", new(31, 20), new(950, 44), 27, Paper);
        _weaponName.ZIndex = 1;
        _path = new QuestPath { Name = "QuestPath", MouseFilter = MouseFilterEnum.Ignore };
        Place(_path, _graph, Vector2.Zero, new(1256, 710));
        Vector2[] positions = [new(237, 293), new(615, 182), new(650, 437)];
        for (var i = 0; i < 3; i++)
        {
            var node = i + 1;
            var button = new Button
            {
                Name = "TaskNode" + node, FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop, MouseDefaultCursorShape = CursorShape.PointingHand
            };
            foreach (var state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
                button.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
            Place(button, _path, positions[i], new(170, 154));
            button.Pressed += () => SelectNode(node);
            button.FocusEntered += Refresh;
            button.FocusExited += Refresh;
            button.MouseEntered += () => _nodePlates[node - 1].SelfModulate = new Color(1.12f, 1.12f, 1.08f);
            button.MouseExited += () => _nodePlates[node - 1].SelfModulate = Colors.White;
            _nodes[i] = button;
            var plate = new Panel { Name = "Plate", MouseFilter = MouseFilterEnum.Ignore };
            Place(plate, button, new(38, 0), new(94, 94));
            _nodePlates[i] = plate;
            if (i < 2)
            {
                var icon = new TextureRect
                {
                    Name = "Weapon", MouseFilter = MouseFilterEnum.Ignore,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
                };
                Place(icon, plate, new(10, 13), new(75, 64));
                _nodeArts[i] = icon;
            }
            else
            {
                var jade = AddLabel(plate, "Jade", "◆", new(12, 13), new(70, 64), 47, Cyan, 3);
                jade.HorizontalAlignment = HorizontalAlignment.Center;
                jade.VerticalAlignment = VerticalAlignment.Center;
            }
            var tick = new Panel { Name = "ClaimedMark", MouseFilter = MouseFilterEnum.Ignore };
            tick.AddThemeStyleboxOverride("panel", Box(new Color("54755a"), new Color("afc5a1"), 2, 16));
            Place(tick, plate, new(73, 72), new(32, 32));
            var check = AddLabel(tick, "Check", "✓", Vector2.Zero, new(32, 32), 23, Paper, 2);
            check.HorizontalAlignment = HorizontalAlignment.Center;
            check.VerticalAlignment = VerticalAlignment.Center;
            _nodeTicks[i] = tick;
            var caption = AddLabel(button, "TaskName" + node, ProgressionUiText.Get("STEP_" + node),
                new(-23, 111), new(216, 43), 27, Paper);
            caption.HorizontalAlignment = HorizontalAlignment.Center;
            _nodeCaptions[i] = caption;
        }
    }

    private void BuildDetail()
    {
        _detail = new Panel { Name = "TaskDetail", Visible = false, MouseFilter = MouseFilterEnum.Stop, ZIndex = 5 };
        _detail.AddThemeStyleboxOverride("panel", Box(new Color("2a4346"), new Color("678379"), 2, 14));
        Place(_detail, _graph, new(864, 40), new(366, 594));
        var margins = new MarginContainer { Name = "DetailMargins", MouseFilter = MouseFilterEnum.Pass };
        _detail.AddChild(margins);
        margins.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margins.AddThemeConstantOverride("margin_left", 24);
        margins.AddThemeConstantOverride("margin_right", 24);
        margins.AddThemeConstantOverride("margin_top", 20);
        margins.AddThemeConstantOverride("margin_bottom", 20);
        var items = new VBoxContainer { Name = "DetailItems", MouseFilter = MouseFilterEnum.Pass };
        items.AddThemeConstantOverride("separation", 6);
        margins.AddChild(items);
        var header = new HBoxContainer { Name = "DetailHeader", CustomMinimumSize = new Vector2(0, 40) };
        items.AddChild(header);
        _step = MakeLabel("Step", "", 27, Gold);
        _step.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(_step);
        _detailClose = MakeButton("DetailClose", "×", 25);
        _detailClose.CustomMinimumSize = new Vector2(34, 34);
        _detailClose.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _detailClose.TooltipText = ProgressionUiText.Get("CLOSE");
        header.AddChild(_detailClose);
        _detailClose.Pressed += CloseDetails;
        _weaponArt = new TextureRect
        {
            Name = "RewardWeapon", CustomMinimumSize = new Vector2(0, 60),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        items.AddChild(_weaponArt);
        _detailWeapon = MakeLabel("RewardWeaponName", "", 20, Paper);
        _detailWeapon.HorizontalAlignment = HorizontalAlignment.Center;
        items.AddChild(_detailWeapon);
        items.AddChild(MakeLabel("TaskHeading", ProgressionUiText.Get("TASK"), 18, Cyan));
        _task = MakeLabel("TaskRequirement", "", 21, Paper);
        _task.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        items.AddChild(_task);
        _status = MakeLabel("TaskProgress", "", 17, Muted);
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        items.AddChild(_status);
        var scroll = new ScrollContainer
        {
            Name = "RewardScroll", CustomMinimumSize = new Vector2(0, 122),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        items.AddChild(scroll);
        var rewardItems = new VBoxContainer { Name = "RewardItems", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        rewardItems.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(rewardItems);
        rewardItems.AddChild(MakeLabel("RewardHeading", ProgressionUiText.Get("REWARD"), 18, Cyan));
        _reward = MakeLabel("RewardDescription", "", 21, new Color("f1dfab"));
        _reward.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _reward.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        rewardItems.AddChild(_reward);
        _claim = MakeButton("Claim", "", 24, true);
        _claim.CustomMinimumSize = new Vector2(0, 53);
        items.AddChild(_claim);
        _claim.Pressed += () => ClaimSelected();
    }

    private void BuildGraphTools()
    {
        var tools = new HBoxContainer { Name = "GraphTools", ZIndex = 6 };
        tools.AddThemeConstantOverride("separation", 9);
        Place(tools, _graph, new(1092, 649), new(144, 42));
        var outButton = MakeButton("ZoomOut", "−", 28);
        var fit = MakeButton("Fit", "↺", 23);
        var inButton = MakeButton("ZoomIn", "+", 28);
        foreach (var button in new[] { outButton, fit, inButton })
        {
            button.CustomMinimumSize = new Vector2(42, 42);
            tools.AddChild(button);
        }
        outButton.Pressed += () => ZoomGraph(_zoom - 0.15f, _graph.Size / 2);
        inButton.Pressed += () => ZoomGraph(_zoom + 0.15f, _graph.Size / 2);
        fit.Pressed += ResetGraph;
    }

    private void ConfigureFocus()
    {
        var weapons = _weaponButtons.Values.ToArray();
        for (var i = 0; i < weapons.Length; i++)
        {
            weapons[i].FocusNeighborTop = weapons[(i + weapons.Length - 1) % weapons.Length].GetPath();
            weapons[i].FocusNeighborBottom = weapons[(i + 1) % weapons.Length].GetPath();
            weapons[i].FocusNeighborRight = _nodes[0].GetPath();
        }
        _nodes[0].FocusNeighborRight = _nodes[1].GetPath();
        _nodes[0].FocusNeighborBottom = _nodes[2].GetPath();
        _nodes[1].FocusNeighborLeft = _nodes[0].GetPath();
        _nodes[1].FocusNeighborBottom = _nodes[2].GetPath();
        _nodes[2].FocusNeighborTop = _nodes[1].GetPath();
        _nodes[2].FocusNeighborLeft = _nodes[0].GetPath();
        _nodes[0].FocusNeighborLeft = weapons[0].GetPath();
    }

    private void Refresh()
    {
        if (_frame == null || _data == null || _detail == null) return;
        var texture = WeaponTexture(ViewedWeapon);
        _weaponName.Text = ProgressionUiText.WeaponName(ViewedWeapon);
        _weaponArt.Texture = texture;
        _detailWeapon.Text = _weaponName.Text;
        _detail.Visible = ViewedNode is >= 1 and <= 3;
        if (_detail.Visible)
        {
            var status = _data.GetStatus(ViewedWeapon, ViewedNode);
            _step.Text = ProgressionUiText.Get("STEP_" + ViewedNode);
            _task.Text = ProgressionUiText.Get(ViewedNode == 3 ? "TASK_3" : "BOSS_" + ViewedNode);
            _reward.Text = ProgressionUiText.Get(ViewedNode == 3 ? "REWARD_SIDE" : $"REWARD.{ViewedWeapon}.{ViewedNode}");
            _status.Text = ViewedNode == 3
                ? string.Format(CultureInfo.InvariantCulture, ProgressionUiText.Get("TASK_PROGRESS3"), Math.Clamp(_data.GetEliteVictories(), 0, 5))
                : !_data.IsEligible(ViewedWeapon) ? ProgressionUiText.Get("UNSELECTED")
                : status == WeaponTaskStatus.Claimable && !_data.CanClaim() ? ProgressionUiText.Get("BATTLE_CLAIM_BLOCKED")
                : status == WeaponTaskStatus.Locked && ViewedNode == 2 ? ProgressionUiText.Get("NEEDS_FIRST")
                : StatusText(status);
            if (ViewedNode == 3)
            {
                var restriction = !_data.IsEligible(ViewedWeapon) ? ProgressionUiText.Get("UNSELECTED")
                    : status == WeaponTaskStatus.Claimable && !_data.CanClaim()
                        ? ProgressionUiText.Get("BATTLE_CLAIM_BLOCKED") : string.Empty;
                if (!string.IsNullOrEmpty(restriction)) _status.Text += "\n" + restriction;
            }
            _claim.Text = status == WeaponTaskStatus.Claimable ? ProgressionUiText.Get("CLAIM") : StatusText(status);
            _claim.Disabled = status != WeaponTaskStatus.Claimable || !_data.CanClaim();
            SetDetailFocus(_detailClose, _claim.Disabled ? _detailClose : _claim);
            SetDetailFocus(_claim, _detailClose);
            _weaponArt.Visible = ViewedNode != 3;
            _detailWeapon.Visible = ViewedNode != 3;
        }
        foreach (var (weapon, button) in _weaponButtons)
        {
            button.AddThemeStyleboxOverride("normal", weapon == ViewedWeapon
                ? NativeBox(_nativeBar, new Color(0.61f, 0.71f, 0.64f), 5)
                : Box(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), 0, 5));
            _weaponNames[weapon].AddThemeColorOverride("font_color", weapon == ViewedWeapon ? new Color("f6da71") : Paper);
        }
        for (var i = 0; i < 3; i++)
        {
            var status = _data.GetStatus(ViewedWeapon, i + 1);
            var active = i + 1 == ViewedNode || _nodes[i].HasFocus();
            var tint = active ? new Color("e6c77f") : status == WeaponTaskStatus.Claimed
                ? new Color("92b995") : status == WeaponTaskStatus.Claimable ? new Color("c8c39c") : new Color("81958d");
            _nodePlates[i].AddThemeStyleboxOverride("panel", NativeBox(_nativeSlot, tint, 0));
            _nodeCaptions[i].AddThemeColorOverride("font_color", active ? new Color("f5d77e") : Paper);
            _nodeTicks[i].Visible = status == WeaponTaskStatus.Claimed;
            if (i < 2) _nodeArts[i].Texture = texture;
        }
        _path.Completed = _data.GetStatus(ViewedWeapon, 1) == WeaponTaskStatus.Claimed;
        _path.QueueRedraw();
    }

    private void OnGraphInput(InputEvent input)
    {
        if (input is InputEventMouseButton button)
        {
            if (button.ButtonIndex == MouseButton.Left)
            {
                _dragging = button.Pressed;
                _dragOrigin = button.Position;
                _panOrigin = _path.Position;
                _graph.AcceptEvent();
            }
            else if (button.Pressed && button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                ZoomGraph(_zoom + (button.ButtonIndex == MouseButton.WheelUp ? 0.08f : -0.08f), button.Position);
                _graph.AcceptEvent();
            }
        }
        else if (input is InputEventMouseMotion motion && _dragging)
        {
            _path.Position = _panOrigin + motion.Position - _dragOrigin;
            _graph.AcceptEvent();
        }
    }

    private void ZoomGraph(float value, Vector2 center)
    {
        var next = Mathf.Clamp(value, 0.65f, 1.55f);
        _path.Position = center - (center - _path.Position) * (next / _zoom);
        _zoom = next;
        _path.Scale = Vector2.One * _zoom;
    }

    public void ResetGraph()
    {
        _zoom = 1;
        _path.Scale = Vector2.One;
        _path.Position = Vector2.Zero;
        _dragging = false;
    }

    private static string StatusText(WeaponTaskStatus status) => ProgressionUiText.Get(status.ToString().ToUpperInvariant());

    private static void SetDetailFocus(Control control, Control next)
    {
        var path = next.GetPath();
        control.FocusNeighborLeft = control.FocusNeighborRight = path;
        control.FocusNeighborTop = control.FocusNeighborBottom = path;
        control.FocusNext = control.FocusPrevious = path;
    }

    private void Layout()
    {
        if (_frame == null) return;
        Layout(GetHudSafeRect());
    }

    private void Layout(Rect2 safeRect)
    {
        _lastSafeRect = safeRect;
        _frame.Visible = safeRect.Size.X > 0 && safeRect.Size.Y > 0;
        if (!_frame.Visible) return;
        var factor = Mathf.Min(safeRect.Size.X / DesignSize.X, safeRect.Size.Y / DesignSize.Y);
        _frame.Scale = Vector2.One * factor;
        _frame.Position = safeRect.Position + (safeRect.Size - DesignSize * factor) / 2;
    }

    private Rect2 GetHudSafeRect()
    {
        // The run's UI uses 1080 design units vertically, regardless of display pixels.
        var nativeUnit = Mathf.Max(0, Size.Y / 1080f);
        var padding = 16 * nativeUnit;
        ResolveHud();
        var top = 170 * nativeUnit;
        if (_hudRoot != null)
        {
            var occupiedBottom = 0f;
            var toScreen = GetGlobalTransformWithCanvas().AffineInverse();
            if (_hudTopBarBackground is { } background && IsVisibleControl(background))
                IncludeHudControl(background, toScreen, ref occupiedBottom);
            if (_hudRelicInventory is { } relics && IsVisibleControl(relics))
                foreach (var child in relics.GetChildren())
                    if (child is Control slot && IsVisibleControl(slot))
                        IncludeHudControl(slot, toScreen, ref occupiedBottom);
            top = occupiedBottom + padding;
        }
        top = Mathf.Clamp(top, 0, Size.Y);
        return new Rect2(new Vector2(padding, top),
            new Vector2(Mathf.Max(0, Size.X - padding * 2), Mathf.Max(0, Size.Y - top - padding)));
    }

    private void ResolveHud()
    {
        if (_hudRoot != null && GodotObject.IsInstanceValid(_hudRoot) && _hudRoot.IsAncestorOf(this))
        {
            if (_hudTopBarBackground == null || !GodotObject.IsInstanceValid(_hudTopBarBackground))
                _hudTopBarBackground = _hudRoot.GetNodeOrNull<Control>("TopBar/BgImage");
            if (_hudRelicInventory == null || !GodotObject.IsInstanceValid(_hudRelicInventory))
                _hudRelicInventory = _hudRoot.GetNodeOrNull<Control>("RelicInventory");
            return;
        }
        _hudRoot = null;
        _hudTopBarBackground = _hudRelicInventory = null;
        for (var ancestor = GetParent(); ancestor != null; ancestor = ancestor.GetParent())
        {
            var background = ancestor.GetNodeOrNull<Control>("TopBar/BgImage");
            var relics = ancestor.GetNodeOrNull<Control>("RelicInventory");
            if (background == null && relics == null) continue;
            _hudRoot = ancestor;
            _hudTopBarBackground = background;
            _hudRelicInventory = relics;
            return;
        }
    }

    private static bool IsVisibleControl(Control control) => GodotObject.IsInstanceValid(control)
        && control.IsVisibleInTree() && control.Size.X > 0 && control.Size.Y > 0;

    private void IncludeHudControl(Control control, Transform2D toScreen, ref float bottom)
    {
        // Do not use TopBar/RelicInventory's full-screen container rectangles as occupied area.
        var transform = toScreen * control.GetGlobalTransformWithCanvas();
        var bounds = new Rect2(transform * Vector2.Zero, Vector2.Zero);
        bounds = bounds.Expand(transform * new Vector2(control.Size.X, 0));
        bounds = bounds.Expand(transform * control.Size);
        bounds = bounds.Expand(transform * new Vector2(0, control.Size.Y));
        if (bounds.End.X <= 0 || bounds.Position.X >= Size.X || bounds.End.Y <= 0 || bounds.Position.Y >= Size.Y)
            return;
        bottom = Mathf.Max(bottom, bounds.End.Y);
    }

    private static Texture2D? LoadTexture(string path) => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

    private static Texture2D? WeaponTexture(WeaponKind weapon) => LoadTexture(weapon == WeaponKind.SovereignBlade
        ? "res://images/atlases/card_atlas.sprites/token/sovereign_blade.tres"
        : $"res://MoreWeaponsRegentExtend/images/vfx/unified/{weapon}.png");

    private static void Place(Control node, Control parent, Vector2 position, Vector2 size)
    {
        parent.AddChild(node);
        node.Position = position;
        node.Size = size;
    }

    private static Label MakeLabel(string name, string text, int font, Color color, int outline = 4)
    {
        var label = new Label { Name = name, Text = text, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", font);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", Outline);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.75f));
        label.AddThemeConstantOverride("outline_size", outline);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        label.AddThemeConstantOverride("shadow_offset_x", 0);
        return label;
    }

    private static Label AddLabel(Control parent, string name, string text, Vector2 position, Vector2 size, int font, Color color, int outline = 4)
    {
        var label = MakeLabel(name, text, font, color, outline);
        Place(label, parent, position, size);
        return label;
    }

    private static void AddRule(Control parent, Vector2 position, Vector2 size, Color color)
        => Place(new ColorRect { Color = color, MouseFilter = MouseFilterEnum.Ignore }, parent, position, size);

    private Button MakeButton(string name, string text, int font, bool nativeBar = false)
    {
        var button = new Button
        {
            Name = name, Text = text, FocusMode = FocusModeEnum.All,
            MouseFilter = MouseFilterEnum.Stop, MouseDefaultCursorShape = CursorShape.PointingHand
        };
        button.AddThemeFontSizeOverride("font_size", font);
        button.AddThemeColorOverride("font_color", Paper);
        button.AddThemeColorOverride("font_hover_color", Gold);
        button.AddThemeColorOverride("font_disabled_color", Muted);
        button.AddThemeColorOverride("font_outline_color", Outline);
        button.AddThemeConstantOverride("outline_size", 4);
        button.AddThemeStyleboxOverride("normal", nativeBar
            ? NativeBox(_nativeBar, new Color(0.66f, 0.66f, 0.54f), 6)
            : Box(new Color("2d454b"), new Color("56766e"), 1, 8));
        button.AddThemeStyleboxOverride("hover", NativeBox(_nativeBar, new Color(0.82f, 0.84f, 0.67f), 6));
        button.AddThemeStyleboxOverride("pressed", NativeBox(_nativeBar, new Color(0.50f, 0.65f, 0.57f), 6));
        button.AddThemeStyleboxOverride("disabled", NativeBox(_nativeBar, new Color(0.42f, 0.52f, 0.47f), 6));
        button.AddThemeStyleboxOverride("focus", Box(new Color(0, 0, 0, 0), new Color("cfba77"), 2, 8));
        return button;
    }

    private static StyleBox NativeBox(Texture2D? texture, Color tint, int padding)
    {
        if (texture == null) return Box(new Color("2c494c"), tint, 1, 9);
        return new StyleBoxTexture
        {
            Texture = texture, ModulateColor = tint,
            TextureMarginLeft = 0, TextureMarginTop = 0, TextureMarginRight = 0, TextureMarginBottom = 0,
            ContentMarginLeft = padding, ContentMarginRight = padding,
            ContentMarginTop = padding, ContentMarginBottom = padding
        };
    }

    private static StyleBoxFlat Box(Color fill, Color border, int width, int radius) => new()
    {
        BgColor = fill, BorderColor = border, BorderWidthLeft = width, BorderWidthTop = width,
        BorderWidthRight = width, BorderWidthBottom = width, CornerRadiusTopLeft = radius,
        CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
        ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 4, ContentMarginBottom = 4
    };

    private partial class QuestPath : Control
    {
        public bool Completed { get; set; }
        public override void _Draw()
        {
            // Only the two mainline nodes are linked. The independent jade task has no edge.
            var start = new Vector2(322, 340);
            var end = new Vector2(700, 229);
            var direction = (end - start).Normalized();
            start += direction * 49;
            end -= direction * 49;
            DrawLine(start, end, new Color("14282e"), 6, true);
            DrawLine(start, end, Completed ? new Color("b7c69c") : new Color("78938a"), 2, true);
        }
    }
}
