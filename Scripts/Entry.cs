using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MoreWeaponsRegentExtend.Scripts.Events;
using MoreWeaponsRegentExtend.Scripts.Progression;
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

        WeaponSelection.Register();
        WeaponProgression.Register();
        WeaponProgression.Changed += WeaponRewardRuntime.RefreshCurrentRun;

        // 注册补丁
        var patcher = RitsuLibFramework.CreatePatcher(ModId, "forge-patches");
        patcher.RegisterPatch<ForgeRandomizePatch>();
        patcher.RegisterPatch<ForgeAsyncEntryPatch>();
        patcher.RegisterPatch<ConquerorModPatch>();
        patcher.RegisterPatch<SwordSageModPatch>();
        patcher.RegisterPatch<SwordSageCardEnteredModPatch>();
        patcher.RegisterPatch<SwordSageRemovedModPatch>();
        patcher.RegisterPatch<SummonForthModPatch>();
        patcher.RegisterPatch<ParryHoverModPatch>();
        patcher.RegisterPatch<WeaponRewardDescriptionPatch>();
        patcher.RegisterPatch<WeaponRewardDamagePreviewPatch>();
        patcher.RegisterPatch<BladeFatalAfterAttackPatch>();
        patcher.RegisterPatch<HandyWeaponStarterDeckPatch>();
        patcher.RegisterPatch<HeavyPressureDamagePatch>();
        patcher.RegisterPatch<CannonAutoPlayGatePatch>();
        patcher.RegisterPatch<CannonPlayWrapperGatePatch>();
        patcher.RegisterPatch<DepartureArmoryAfterNeowPatch>();
        patcher.RegisterPatch<DepartureArmoryRoomRestorePatch>();
        patcher.RegisterPatch<DepartureArmoryProceedPatch>();
        if (!patcher.PatchAll())
            throw new InvalidOperationException("Forge patches failed.");

    }
}
