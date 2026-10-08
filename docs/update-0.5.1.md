# 0.5.1 验收调整与兵器库补图

日期：2026-10-05。作者已实机确认 0.5.0 各武器效果正确实现、对应卡图全部通过；本轮据此调整三项效果并补齐兵器库插画。0.5.0 已验收反馈不意味着 0.5.1 新改动已经实机验收。

## 逐项改动

| 内容 | 0.5.1 行为 | 主要文件 |
| --- | --- | --- |
| 银河轨迹炮 | 默认攻击所有敌人；追踪之刃使整张牌额外打出一次。沿用原版重复出牌框架，两个出牌各触发一次群攻、出牌钩子及附加效果，资源支付与最终消耗仅一次。其他重复出牌次数继续相加 | `Scripts/Cards/GalaxyTrajectoryCannon.cs` |
| 光之圣典 | 基础赐福从 0 调为 1，打出时赐福次数为 `1 + floor(自身累计铸造 / 10)`；0/9/10/20 铸造时分别为 1/1/2/3。七种奖励及各自数值保持当前设计 | `Scripts/Cards/HolyCodex.cs` |
| 皇家指虎／武痴 | 每段实际突破格挡的伤害触发铸造从 10 降为 8；适应仍每层增减伤 20%、最高四层 | `Scripts/Powers/MartialFanaticPower.cs` |
| 轻巧兵器库 | 重绘并完整展示晶刃、短匕、三叉戟、天皇太刀和定海圣棍五件武器 | `images/events/DepartureArmoryLight.png` |
| 远程兵器库 | 重绘并展示邪皇圣弓、皇室猎枪、银河轨迹炮三件武器，延续已有武器身份及配色 | `images/events/DepartureArmoryRanged.png` |
| 特殊兵器库 | 重绘并展示君王之翼、光之圣典和皇家指虎三件武器 | `images/events/DepartureArmorySpecial.png` |

相关中文描述同步更新：炮明确“所有敌人”及追踪之刃的额外出牌，圣典直接显示当前赐福总次数，武痴显示铸造 8。版本号升为 0.5.1。

武器目录仍为 16 件主武器，包含轻巧5、厚重5、远程3、特殊3；沿用最多四选项的自动分页。充能仍各自绑定一张炮的三个独立槽位，冷炮不能手动或自动打出。卡图、悬浮素材、轨道和场景沿用已验收版本。本轮没有启动全局养成系统。

## 验证与安装包

主项目编译和打包 **0 警告、0 错误**。实际 DLL 夹具 **155 项断言全部通过**，Godot 退出 0。验证直接引用最终 DLL，并校验加载模块 MVID 与 SHA256，没有重写生产效果。

- 真实 `CardCmd.AutoPlay` 使用空目标对双敌群攻；实际 `SpendResources` + manual `OnPlayWrapper` 产生两个 `CardPlay` 和四个受伤结果，能量只扣 3，一次 `EnergySpentEntry`、一次 `CardExhaustedEntry`。
- 双炮同场、热原炮 `CreateDupe` 和正常炮都恰好增加一次追踪重放；`BaseReplayCount=2`／真实剑术大师 2 与追踪之刃相加为四次。来源炮不再重复响应临时重放牌，避免错误的三次出牌。
- 冷炮、冷复制和它们的重放副本仍拒绝 AutoPlay／manual wrapper，伪造显示热状态和额外重复次数不能绕过实际三槽充能。
- 圣典 F=0/9/10/20 的真实赐福与变量为 1/1/2/3，真实出牌与 RNG 奖励次数、七种实际 PowerCmd 分支、复制独立和降级变量恢复通过。
- 武痴每段突破格挡的伤害实际铸造 8，多段各计一次，完全格挡不增加铸造；适应上限和各层倍率保持通过。原圣棍、三槽充能、首次铸造等回归保留。
- 真实中文 LocString／SmartFormatter 验证群攻、额外出牌、赐福次数和铸造 8 文案，无裸占位符。

夹具使用最小反射 Player／Run／战斗容器和真实 `CombatState.IterateHookListeners`；仅通过 postfix 追加尚未登记的 ForgeSingleton 并去重，替代活跃战斗终止门槛以支持离线 fixture，效果、卡牌、伤害、Hook、PowerCmd、ForgeCmd 和怪物动作仍为实际实现。没有驱动完整游戏回合调度、UI、网络或真实存档写盘。详见 `.build/update-0.5.1-validation/README.md`，日志为同目录 `build.log`、`verify.log`。

资源核验通过：正式 PCK 中 20 卡面、20 施放、18 悬浮、8 状态图标、5 事件插画与 16 武器目录均能加载，原版轨道／方向／居中／Hitbox 检查通过。三幅新插画经过主代理实际视检，并以原始文件哈希和四个原生解码像素点确认包内来源及导入资源确为新图。完整提示词、生成来源与审批见 [美术审批](art-review-0.5.1.md)。

证据：`.build/acceptance-0.5.1-build.log`、`acceptance-0.5.1-art-pack.log`、`acceptance-0.5.1-armory-pack.log`、`acceptance-0.5.1-armory-source.log`、`acceptance-0.5.1-zip-verify.log`（后四项同在 `.build/`）。ZIP 仅含模组 DLL/PCK/JSON 三文件，版本 0.5.1，各字节与构建产物一致；没有向游戏安装目录部署或自动复制 RitsuLib。

安装包：[MoreWeaponsRegentExtend-0.5.1.zip](../.build/MoreWeaponsRegentExtend-0.5.1.zip)，**118,429,356 字节**。ZIP SHA256：`647D82AEDB42F0B0BE1DDF71FCC934EBC0C2575B3DBBB59DDD10B4B44F1A0CC0`。

| 文件 | SHA256 | 字节数 |
| --- | --- | --- |
| MoreWeaponsRegentExtend.dll | `8C932AC7192831DDD347D73C5EACAF5F5ECD4A8D254445DC773BC2B28F1CFE4A` | 174,080 |
| MoreWeaponsRegentExtend.pck | `7CB8251621D658FC6BD88C6311E1F298446C4103B6FFE7F0A9F7D9B642BCDCF2` | 121,293,916 |
| MoreWeaponsRegentExtend.json | `0FE818648E4C39A13F068AE4AB6A8F1344123056FBD7A2AB842E76B0520D5231` | 343 |

## 游戏复验

1. 在多个存活敌人面前打出已热启的轨迹炮：无追踪之刃时全部受击一次，有追踪之刃时每个仍存活的敌人受击两次。牌只消耗一次、只支付一次资源；剑术大师等额外出牌继续相加。
2. 同场有两张炮时，追踪之刃只为正在打出的炮增加一次出牌；另一张仍保留自己的充能和热状态。冷炮继续拒绝所有出牌入口。
3. 圣典未铸造时赐福 1；自身累计铸造 10/20 后分别为 2/3，文字与实际奖励一致。
4. 指虎每段突破格挡的伤害铸造 8；完全格挡仍获得适应，上限四层。
5. 轻巧、远程、特殊分类均显示补齐后的插画，武器数量及结构对应目录，翻页与返回正常。

历史与当前状态见 [0.5.0 记录](update-0.5.0.md) 和 [进度](progress.md)。
