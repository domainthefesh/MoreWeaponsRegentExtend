# 0.3.0 出发前选兵器

## 改动

1. 储君完成开局涅奥事件后进入兵器库；普通地图事件不随机抽取此事件。
2. 轻巧分支：君王晶刃、君王短匕、海王三叉戟、太刀。
3. 厚重分支：君王镰刀、君王重斧、君王重锤、君王长矛、君王之盾。
4. 远程分支：天启长弓、君王新枪；特殊分支：君王之翼。
5. 随心所欲：随机选一把，固定整局，直接结束选择；百武皆通：每次铸造重新随机，直接结束选择。
6. 选择以 RitsuLib PlayerRunSavedData 保存到本局，玩家各自独立；随机流以本局玩家种子与已保存计数恢复。旧存档没有选择时沿用首次铸造随机的兼容行为。
7. 移除原配置页的铸造模式与武器池开关，旧 forge_settings.json 不再读取。
8. 母事件与四个分支使用五张独立插画；子页可返回重新选分类。
9. 君王长矛悬浮贴图顺时针转 90°，同步悬停范围；太刀悬浮刀身改为近直轻弧，保留已验收配色与画风。

选定兵器影响铸造生成种类，武器已有的效果仍生效，例如君王新枪生成子弹、晶刃产生水晶小刀。

## 游戏内验收

- 新开储君单人局，完成涅奥选项后应立即出现“出发前选兵器”，未选武器时不应直接打开地图。
- 检查六个入口及四组武器名单，逐页确认插画切换、“这次”红色加重、返回分类按钮。
- 选定任一武器，多次铸造、换战斗、跨章节后仍只生成该武器；已有武器继续累计铸造。
- 选择“随心所欲”直接完成，检查结果页显示选中的武器，后续整局固定该武器。
- 选择“百武皆通”直接完成，反复铸造应生成不同武器。
- 在兵器库选择前或选择后保存退出，读档应回到可选择的事件或已选择的完成页；进入地图后再读档不应重复弹出。
- 非储君单人局不触发；联机中各储君应独立选择，所有玩家完成后正常继续。
- 查看君王长矛朝右、太刀刀身近直，攻击动画结束后正常复位。

## 构建与检查

- 主模组编译通过，0 警告、0 错误；构建日志：`.build/armory-build.log`。
- 独立 Godot headless 验证 156 条断言通过，包含实际 RitsuLib 玩家数据导出/恢复、两玩家隔离、随机序列续接、12 种武器与三个模式、ActFloor=1 的涅奥入口、补丁目标反射核对。工程与复现命令：`.build/armory-validation/README.md`，日志：`.build/armory-validation/verify.log`。
- 发布资源包通过 5 张事件图、6 个入口、4 组武器、12 个武器文本检查：`.build/armory-pack-verify.log`。
- 原有 15 张卡图、15 个使用特效、14 个悬浮场景、4 个状态图标和居高临下文本检查通过：`.build/armory-art-verify.log`。
- ZIP 包三个文件与构建产物 SHA256 一致：`.build/armory-zip-verify.log`。
- 尚未进游戏验证 UI、涅奥实际切换、SaveManager 写盘 hook、联机网络传输及启动注册后的模型 ID。上面的 headless 验证使用最小 run/player fixture，并不能替代这些游戏验收。

打包文件：`.build/MoreWeaponsRegentExtend-0.3.0.zip`。解压后用其中的 MoreWeaponsRegentExtend 文件夹覆盖本模组的游戏安装目录。构建过程仅写项目内文件，没有部署到游戏或改变依赖库安装。

## 美术

内置 image_gen 生成事件插画与太刀编辑，提示词见 armory-event-art-prompts.md、katana-straighten-prompt.md。正式事件图保存于 `MoreWeaponsRegentExtend/images/events/`：

- `DepartureArmoryInitial.png`
- `DepartureArmoryLight.png`
- `DepartureArmoryHeavy.png`
- `DepartureArmoryRanged.png`
- `DepartureArmorySpecial.png`

均为 1536×1024 PNG。太刀文件为 `MoreWeaponsRegentExtend/images/vfx/unified/SovereignKatana.png`，旧图备份在 `.build/art-before-armory/`。

## 2026-10-04：事件未出现的实际部署核查

本机游戏目录 `D:/steam/steamapps/common/Slay the Spire 2/mods/MoreWeaponsRegentExtend/` 仍为 0.2.4。该目录 DLL 和 PCK 的 SHA256 都与 0.2.4 安装包完全一致，并非只是清单版本遗漏；0.3.0 包的 DLL/PCK 与它们不同。当前储君存档从涅奥继续进入战斗，符合旧版未包含兵器库事件的行为。角色实际为 CHARACTER.REGENT，人物皮肤不影响本次判断。

本轮只读核查，未改动游戏目录或存档。项目内构建不会自动部署到游戏。更新时必须从 0.3.0 ZIP 取出下列三个文件，一起覆盖游戏中的本模组目录；仅导出/覆盖 PCK 不会更新 C# 事件代码：

- MoreWeaponsRegentExtend.dll
- MoreWeaponsRegentExtend.json
- MoreWeaponsRegentExtend.pck

覆盖后重启游戏，确认清单版本为 0.3.0，重新开始储君局验证。已经离开涅奥进入战斗的旧存档不会补弹开局选择；其铸造使用旧存档兼容行为。
