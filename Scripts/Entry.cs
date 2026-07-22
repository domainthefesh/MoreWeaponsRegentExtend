using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;

namespace MoreWeaponsRegentExtend.Scripts;

[ModInitializer(nameof(Init))]
public class Entry
{
    // 你的modid
    public const string ModId = "MoreWeaponsRegentExtend";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        // 自动注册内容
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // 注册补丁
        var patcher = RitsuLibFramework.CreatePatcher(ModId, "forge-patches");
        patcher.RegisterPatch<ForgeRandomizePatch>();
        patcher.RegisterPatch<ConquerorModPatch>();
        patcher.RegisterPatch<SwordSageModPatch>();
        patcher.RegisterPatch<SwordSageCardEnteredModPatch>();
        patcher.RegisterPatch<SwordSageRemovedModPatch>();
        patcher.RegisterPatch<SummonForthModPatch>();
        patcher.RegisterPatch<ParryHoverModPatch>();
        if (!patcher.PatchAll())
            throw new InvalidOperationException("Forge patches failed.");

        // 注册设置页面
        MoreWeaponsSettingsPage.Register();
    }
}
