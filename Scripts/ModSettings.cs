using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace MoreWeaponsRegentExtend.Scripts;

public sealed class MoreWeaponsSettings
{
    // 1. 首次铸造随机生成（主开关）
    public bool FirstForgeRandomEnabled { get; set; } = true;

    // 2–9. 各武器独立开关
    public bool EnableSovereignBludgeon { get; set; } = true;
    public bool EnableApocalypseLongbow { get; set; } = true;
    public bool EnableSovereignCrystalBlade { get; set; } = true;
    public bool EnableSovereignShield { get; set; } = true;
    public bool EnableSovereignSpear { get; set; } = true;
    public bool EnableSovereignAxe { get; set; } = true;
    public bool EnableSovereignGun { get; set; } = true;
    public bool EnableSovereignScythe { get; set; } = true;

    // 10. 每次铸造都随机生成
    public bool EveryForgeRandom { get; set; } = false;
}

public static class MoreWeaponsSettingsPage
{
    private const string DataKey = "forge_settings";

    private static readonly ModSettingsValueBinding<MoreWeaponsSettings, bool> FirstForgeRandomBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => s.FirstForgeRandomEnabled,
        static (s, v) => s.FirstForgeRandomEnabled = v);

    private static readonly ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableBludgeonBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => s.EnableSovereignBludgeon,
        static (s, v) => s.EnableSovereignBludgeon = v);

    private static readonly ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableLongbowBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => s.EnableApocalypseLongbow,
        static (s, v) => s.EnableApocalypseLongbow = v);

    private static readonly ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableCrystalBladeBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => s.EnableSovereignCrystalBlade,
        static (s, v) => s.EnableSovereignCrystalBlade = v);

    private static readonly ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableShieldBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => s.EnableSovereignShield,
        static (s, v) => s.EnableSovereignShield = v);

    private static readonly ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableSpearBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => s.EnableSovereignSpear,
        static (s, v) => s.EnableSovereignSpear = v);

    private static readonly ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableAxeBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => s.EnableSovereignAxe,
        static (s, v) => s.EnableSovereignAxe = v);

    private static readonly ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableGunBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => s.EnableSovereignGun,
        static (s, v) => s.EnableSovereignGun = v);

    private static readonly ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableScytheBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => s.EnableSovereignScythe,
        static (s, v) => s.EnableSovereignScythe = v);

    private static readonly ModSettingsValueBinding<MoreWeaponsSettings, bool> EveryForgeRandomBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => s.EveryForgeRandom,
        static (s, v) => s.EveryForgeRandom = v);

    // 公开绑定供 ForgePatches 读取
    public static ModSettingsValueBinding<MoreWeaponsSettings, bool> FirstForgeRandom => FirstForgeRandomBinding;
    public static ModSettingsValueBinding<MoreWeaponsSettings, bool> EveryForgeRandom => EveryForgeRandomBinding;
    public static ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableBludgeon => EnableBludgeonBinding;
    public static ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableLongbow => EnableLongbowBinding;
    public static ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableCrystalBlade => EnableCrystalBladeBinding;
    public static ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableShield => EnableShieldBinding;
    public static ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableSpear => EnableSpearBinding;
    public static ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableAxe => EnableAxeBinding;
    public static ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableGun => EnableGunBinding;
    public static ModSettingsValueBinding<MoreWeaponsSettings, bool> EnableScythe => EnableScytheBinding;

    public static void Register()
    {
        ModDataStore.For(Entry.ModId).Register<MoreWeaponsSettings>(
            key: DataKey,
            fileName: "forge_settings.json",
            scope: SaveScope.Profile,
            defaultFactory: () => new MoreWeaponsSettings(),
            autoCreateIfMissing: true);

        RitsuLibFramework.RegisterModSettings(Entry.ModId, page => page
            .WithTitle(ModSettingsText.Literal("百般武艺"))
            .WithModDisplayName(ModSettingsText.Literal("百般武艺-储君扩展"))
            .WithVisibleOnHostSurfaces(ModSettingsHostSurface.MainMenu | ModSettingsHostSurface.RunPause)
            .AddSection("forge", section => section
                .WithTitle(ModSettingsText.Literal("铸造设置"))
                .AddToggle("first_forge_random", ModSettingsText.Literal("首次铸造随机生成武器"), FirstForgeRandomBinding)
                .AddToggle("every_forge_random", ModSettingsText.Literal("每次铸造都随机生成武器"), EveryForgeRandomBinding))
            .AddSection("weapons", section => section
                .WithTitle(ModSettingsText.Literal("可生成的武器"))
                .AddToggle("enable_bludgeon", ModSettingsText.Literal("君王重锤"), EnableBludgeonBinding)
                .AddToggle("enable_longbow", ModSettingsText.Literal("天启长弓"), EnableLongbowBinding)
                .AddToggle("enable_crystal_blade", ModSettingsText.Literal("君王晶刃"), EnableCrystalBladeBinding)
                .AddToggle("enable_shield", ModSettingsText.Literal("君王之盾"), EnableShieldBinding)
                .AddToggle("enable_spear", ModSettingsText.Literal("君王长矛"), EnableSpearBinding)
                .AddToggle("enable_axe", ModSettingsText.Literal("君王重斧"), EnableAxeBinding)
                .AddToggle("enable_gun", ModSettingsText.Literal("君王新枪"), EnableGunBinding)
                .AddToggle("enable_scythe", ModSettingsText.Literal("君王镰刀"), EnableScytheBinding)));
    }

    // 便捷方法：根据开关返回启用的武器类型列表
    public static List<int> GetEnabledWeaponIndices()
    {
        var indices = new List<int>();
        // 顺序：Bludgeon=0, Longbow=1, CrystalBlade=2, Shield=3, Spear=4, Axe=5, Gun=6, Scythe=7
        if (EnableBludgeonBinding.Read()) indices.Add(0);
        if (EnableLongbowBinding.Read()) indices.Add(1);
        if (EnableCrystalBladeBinding.Read()) indices.Add(2);
        if (EnableShieldBinding.Read()) indices.Add(3);
        if (EnableSpearBinding.Read()) indices.Add(4);
        if (EnableAxeBinding.Read()) indices.Add(5);
        if (EnableGunBinding.Read()) indices.Add(6);
        if (EnableScytheBinding.Read()) indices.Add(7);
        return indices;
    }
}
