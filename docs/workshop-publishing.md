# Steam 创意工坊发布与更新

核实日期：2026-10-05。本轮发布包版本为 **0.5.3**，文件为 `.build/MoreWeaponsRegentExtend-0.5.3.zip`。**作者已确认上传本轮工坊更新，本轮收尾。** 本文保留操作说明供后续更新使用。新封面、双语介绍及更新说明见 [本轮发布素材](release-0.5.3/README.md)。

## 本轮建议：更新已有条目

当前更新仍延续同一储君武器模组，建议保留已有工坊条目及其数字 ID。可以在页面标题、介绍和更新说明中使用“全面更新”或作者选定的宣传名称，无需为此建立新条目；宣传名称与发布包版本分别管理，当前包标为 0.5.3。

更新已有条目后，原订阅者继续订阅同一条目，Steam 会按工坊更新机制下载新版。只有准备长期分别维护旧版玩法和独立新版时，才建议另发一个 2.0 条目。[Valve 工坊安装与更新机制](https://partner.steamgames.com/doc/features/workshop/implementation#ItemInstallation)

作者本轮截图显示：1,443 个不重复访客，108 个当前订阅，355 个历史不重复订阅，46 个当前收藏，5 个评价（3 好评、2 差评）。108 不是历史累计订阅数；这些计数没有提供逐人、同一时间窗口的访问到订阅路径，不能据此断言准确转化率。仅 5 个评价也不足以代表当前全面重做版本的质量。

更新原条目能继续触达已有订阅并保留收藏和评论。Steam 的官方接口提供按发布时间、最近更新时间及一段时期投票热度排序，三者是不同规则；更新不会把原条目的创建日期改为十月，也不代表自动获得热门推荐或通知全部访问过的人。新发布有新的创建时间，但必须重新积累订阅、收藏及评价，同样不保证曝光。[Valve 工坊查询排序](https://partner.steamgames.com/doc/api/ISteamUGC#EUGCQuery)

建议把这次更新作为一次完整重新展示：替换封面，使用双语介绍，配几张新版实机截图和短演示，明确列出“16 件武器、开局选兵器、统一美术、中英文本、原版联动”。作者可在平时使用的相关社区分享这次更新；本项目未代发评论或宣传信息。发布后一至两周记录新增访问、订阅与实际反馈，再决定后续宣传和开发投入。

## 准备上传工具和文件

使用 [Mega Crit 官方 ModUploader 下载页](https://github.com/megacrit/sts2-mod-uploader/releases)中的 Windows 版本。以下示例路径需要替换为你的实际完整路径；代码块仅供你手动运行。无需安装 SteamCMD。[官方上传说明](https://github.com/megacrit/sts2-mod-uploader/blob/main/README.md)

登录拥有现有模组条目的 Steam 账号，并保持 Steam 客户端运行。找到首次发布时使用的上传工作区。解压本轮 ZIP，将下列三份新版文件替换到工作区的 `content` 文件夹中：

- `MoreWeaponsRegentExtend.dll`
- `MoreWeaponsRegentExtend.pck`
- `MoreWeaponsRegentExtend.json`

**上传的是这三份文件，不是 ZIP 压缩包。** 不要将源码、`.build`、本地开发依赖库或测试文件一起放入 `content`。

示例工作区结构：

```text
D:\模组上传工作区\MoreWeaponsRegentExtend\
├─ content\
│  ├─ MoreWeaponsRegentExtend.dll
│  ├─ MoreWeaponsRegentExtend.pck
│  └─ MoreWeaponsRegentExtend.json
├─ workshop.json
├─ image.png
└─ mod_id.txt
```

`image.png` 是工坊预览图，必须存在且小于 1 MB。本轮使用 `docs/release-0.5.3/image.png`（768×768，873,977 字节），将其复制到工作区根目录；高清 `cover-master.png` 留作原稿。中英介绍与更新说明的文本同样保存在该目录。[官方工作区说明](https://github.com/megacrit/sts2-mod-uploader/blob/main/template/README.md)

## 更新已有工坊条目

1. 保留原工作区的 `mod_id.txt`，其中记录已有条目的数字 ID。
2. 更新 `content` 中的三份文件；在 `workshop.json` 的 `changeNote` 中填写本轮更新内容。
3. 沿用原公开状态。已公开的条目应保持 `"visibility": "public"`；新模板默认是 `private`，直接套用模板会改变条目可见性。
4. 保留 RitsuLib 依赖：`"dependencies": [3747602295]`。不要改成空数组。
5. 在 PowerShell 手动执行：

```powershell
& "D:\发布工具\STS2-ModUploader\ModUploader.exe" upload -w "D:\模组上传工作区\MoreWeaponsRegentExtend"
```

上传器读取 `mod_id.txt`，更新该 ID 对应的原条目。[官方更新流程](https://github.com/megacrit/sts2-mod-uploader/blob/main/README.md)

### 原工作区或 mod_id.txt 丢失

重新创建工作区，准备相同结构的文件。从已有工坊页面网址的 `?id=` 后取得数字 ID，明确传入 `--id`：

```powershell
# 将 1234567890 替换为你已有模组条目的真实数字 ID。
& "D:\发布工具\STS2-ModUploader\ModUploader.exe" upload -w "D:\模组上传工作区\MoreWeaponsRegentExtend" --id 1234567890
```

`--id` 优先于工作区的 `mod_id.txt`。成功后，上传器会把使用的条目 ID 写回该文件。恢复工作区时也要检查公开状态和依赖。[官方参数定义](https://github.com/megacrit/sts2-mod-uploader/blob/main/src/Program.cs)、[官方上传实现](https://github.com/megacrit/sts2-mod-uploader/blob/main/src/UploadCommand.cs)

## 如果决定另发独立 2.0

1. 双击 `ModUploader.exe` 创建 `NewModWorkspace`，将其重命名为新的上传工作区，保留旧工作区。
2. 在新工作区准备 `content`、`workshop.json` 和小于 1 MB 的 `image.png`。填写新标题、介绍、公开状态和 RitsuLib 依赖。
3. **新工作区不携带旧 `mod_id.txt`，上传命令不传 `--id`。** 上传器在没有这两种已有 ID 来源时才创建新条目。
4. 手动运行同样的上传命令，路径改为新工作区。首次公开若提示接受工坊协议，按提示完成。

```powershell
& "D:\发布工具\STS2-ModUploader\ModUploader.exe" upload -w "D:\模组上传工作区\MoreWeaponsRegentExtend-2.0"
```

上传成功后会生成新的 `mod_id.txt`，此后用这个工作区执行上传即更新新条目。[官方新建流程](https://github.com/megacrit/sts2-mod-uploader/blob/main/README.md)、[官方 ID 选择逻辑](https://github.com/megacrit/sts2-mod-uploader/blob/main/src/Program.cs)

工坊条目 ID 与游戏内部模组 ID 是两回事。另建条目不会自动把运行时 manifest 的 `"id": "MoreWeaponsRegentExtend"` 改为另一套身份。如果两条目仍使用同一运行时身份，玩家应**二选一启用**。如果要求两个版本真正同时加载，需要完整处理模组 ID、程序集和资源路径、模型注册、补丁及存档身份；仅改文件名或 JSON 的一个 ID 不足以实现并存。

## RitsuLib 依赖的两种 ID

`workshop.json` 管理 Steam 条目，因此依赖采用 **工坊数字 ID**：

```json
"dependencies": [3747602295]
```

该数字对应 [OLC 发布的 RitsuLib 工坊条目](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295)。运行时文件 `MoreWeaponsRegentExtend.json` 管理游戏加载，因此仍使用 **模组字符串 ID**：

```json
"dependencies": [
  { "id": "STS2-RitsuLib" }
]
```

两个字段不能互换。工坊 `content` 只放本模组三份发布文件，玩家通过工坊依赖安装 RitsuLib。[官方 dependencies 字段说明](https://github.com/megacrit/sts2-mod-uploader/blob/main/template/README.md)

## 上传后检查

1. 更新原条目时确认打开的仍是原工坊网址、原数字 ID，没有额外新建条目；新发布时记录新网址与 ID。
2. 检查页面公开状态、更新说明及必需项目列表，确认 RitsuLib 仍在列表中。
3. 等 Steam 的工坊下载完成后，关闭并重新启动游戏；用订阅版本测试时，避免同时启用本地开发版。
4. 检查订阅下载中的 `.dll`、`.pck`、`.json` 是否为本轮同一套文件，manifest 版本是否为 0.5.3。
5. 切换英文语言，新开储君局检查兵器库及武器长名称、架势和充能状态、Parry 动态文本及能力悬停；中文切回后应保持原效果。Parry 与武器联动已获作者本地实机确认，订阅版本再做简单回归，并留意卡框资源日志。
6. 如上传失败，先查看上传工具产生的 `mod-uploader.log`。[官方日志说明](https://github.com/megacrit/sts2-mod-uploader/blob/main/README.md)

## SteamCMD 是否需要

本项目推荐官方 ModUploader。STS2 的 AppID 为 **2868840**，上传器已经内置。Valve 的 SteamCMD 通用机制也支持创建或更新工坊条目，但其文档将该方式定位为测试用途，本指南不要求使用它。[官方上传器 AppID](https://github.com/megacrit/sts2-mod-uploader/blob/main/src/UploadCommand.cs)、[Valve SteamCMD 说明](https://partner.steamgames.com/doc/features/workshop/implementation#SteamCmdIntegration)
