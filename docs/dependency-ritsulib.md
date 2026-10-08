# RitsuLib 重复安装核查

核查日期：2026-10-03；2026-10-04 续作复核清单，本地 0.6.2、工坊 0.6.5、游戏 0.111.0 均未改变。只读检查，未修改、删除或移动游戏目录及工坊文件，也未改变 Steam 订阅。

| 用途 | 确切路径 | 身份与版本 |
| --- | --- | --- |
| 可审核移除的本地运行副本 | `D:/steam/steamapps/common/Slay the Spire 2/mods/STS2-RitsuLib` | manifest ID `STS2-RitsuLib`，RitsuLib 0.6.2 |
| 保留的工坊运行副本 | `D:/steam/steamapps/workshop/content/2868840/3747602295` | manifest ID `STS2-RitsuLib`，RitsuLib 0.6.5 |
| 保留的编译缓存 | `C:/Users/Halloween/.nuget/packages/sts2.ritsulib/0.6.2` | NuGet `STS2.RitsuLib` 0.6.2；不可作为重复运行副本删除 |

本机游戏 `release_info.json` 为 v0.111.0。工坊包包含 0.107.1、0.109.0、0.110.0、0.111.0 兼容变体，本地包包含 0.111.0。两份变体清单声明的 DLL SHA256 均与实际文件一致。工坊运行库版本 0.6.5.0 高于本模组编译引用的 0.6.2.0；其加载器允许较旧引用。模组清单只要求依赖 ID `STS2-RitsuLib`，未锁定精确版本。因此该工坊包具备本机游戏及本模组所需的版本条件，尚未据此完成一次实际进游戏加载验证。

实际游戏 `ModManager.RemoveDisabledMods()` 比较同 ID 的本地与工坊版本，选择较高版本，版本相同时优先本地。按当前版本，在两份均启用且工坊被正常发现的前提下，应保留工坊 0.6.5、将本地 0.6.2 标记为重复禁用；重复安装本身不会让两份同时失效。用户禁用设置按 ID 和来源分别匹配。

项目通过 NuGet `PackageReference STS2.RitsuLib 0.6.2` 编译，未引用游戏目录中的 RitsuLib。因此移除本地运行副本不会影响编译；工坊目录与 NuGet 编译缓存须保留。项目已将 `RitsuLibAutoCopy` 固定为 `false`，显式 `DeployToGame=true` 只部署本模组，不会重新创建本地 RitsuLib 副本。

局限：已知游戏日志目录和账户 `settings.save` 路径访问被拒绝（Node EPERM，.NET Directory 同样确认拒绝），无法读取实际禁用设置或加载错误，不能声称有“两份都未加载”的日志证据。未触发 Steam 更新或联网查询工坊支持范围。详细清单、哈希和实际 DLL 反编译证据保存于 `.build/dependency-audit/`。
