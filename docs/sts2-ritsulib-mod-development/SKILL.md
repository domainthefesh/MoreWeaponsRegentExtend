---
name: sts2-ritsulib-mod-development
description: 为杀戮尖塔2的RitsuLib模组建立工程、制作卡牌与其他内容、接入Godot美术和界面，并完成本地化、验收与发布。用于新模组起步或现有模组迭代；按实际游戏与依赖版本核对接口，不继承其他模组的具体玩法。
---

# STS2／RitsuLib 模组制作：可迁移开发规范与教程

整理日期：2026-10-09。本文综合实际协作、源码与依赖接口核查、验收反馈及公开教程，独立成文；迁移时不需要原项目的卡牌代码、美术文件、测试夹具或绝对路径。

## 0. 如何迁移和使用

只需迁移本文件，按用途二选一：

- **项目约束与教程**：复制到新项目根目录，命名为 `AGENTS.md`。保留全文即可，开发助手按当前任务查阅相关章节。
- **独立技能**：保存为新项目的 `.agents/skills/sts2-ritsulib-mod-development/SKILL.md`，在支持项目技能发现的环境中使用。必要时在新会话明确要求读取或使用 `$sts2-ritsulib-mod-development`。没有技能发现功能时直接让助手读取这个文件。

不需要同时安装两份，不自动安装其他技能，不迁移原项目的缓存、解包资源、模型 ID 或发布身份。本文件有 YAML 技能头；作为普通 Markdown／AGENTS 使用时同样可读。

开始新项目时，先填写一份简短项目说明：模组 ID、显示名称、目标角色／内容、目标游戏版本、平台、安装目录、Godot 和 .NET 版本、RitsuLib 编译包版本、运行库来源、修改目录范围、本轮交付物和游戏内验收条件。下文示例中的 `ExampleMod` 是新项目占位名，须统一替换。

阅读路径：

| 当前任务 | 优先阅读 |
| --- | --- |
| 建立工程和第一次加载 | 第1～4章、第12章 |
| 卡牌、能力、衍生物 | 第5～7章 |
| 事件与开局流程 | 第8章 |
| 任务／养成／其他独立页面 | 第9～10章 |
| 卡图、特效、动画或UI | 第7章、第9章、第11章 |
| 修复原版联动、异步钩子 | 第6章、第13章 |
| 本地化、打包或发行 | 第7章、第12～14章 |

本文包含三种不同性质的信息：**作者持续约定**需要遵守；**通用实现经验**按功能采用；**版本快照与示例接口**必须在新项目重新核实。没有明确采用的新玩法，不能从旧项目推导成新项目要求。

## 1. 作者持续约定和协作方式

1. **先给计划，再改代码。** 计划交代实现范围、参考、关键机制、素材与验收方式；完成后逐项说明改动、验证结果及未验证范围。
2. **默认基于 STS2-RitsuLib，不擅自增加其他模组运行依赖或第三方库。** Godot、.NET、游戏自带 Harmony 和已选 RitsuLib 属于既定基础。开发工具与玩家运行依赖分开描述；不要用“装了工具”掩盖新增运行依赖。
3. **仅修改用户指定工作区。** 构建、临时脚本、截图、日志和安装包集中放到项目内部的 `.build/` 等目录。复制到游戏、修改外部依赖或发布上传，须有该动作的授权；不要把自动部署藏进普通构建步骤。
4. **已授权的制作与修复连续完成。** 不迁移旧 skill 中“每个步骤都再次询问许可”的做法。只对影响设计的真实歧义提问；等待期间继续不依赖答案的工作，不能将等待超时视为用户同意。
5. **按需查参考。** 优先本项目相似实现和已有教程，缺少时查目标版本原版实现、RitsuLib文档及源码。用 `rg` 限定类型／方法搜索，不大范围无目的读取反编译目录。
6. **美术采用前实际审图。** 制作方先自查，负责整合的助手逐张打开复审，确认结构与小尺寸表现后放入正式路径。需要作者选画风时提交可比较的样稿，作者决定采用方向；不要自行把所有候选都实装。
7. **保持色彩和线条干净。** 默认贴近原版体验，删去无辨识作用的装饰细节。具体主题、画风和UI风格由新项目决定，不沿用旧项目的样式字母、配色或武器外观。
8. **中文和英文分别维护。** 游戏文本使用对应语言表；工坊简介和更新日志按语言分文件。英文版是对应中文稿的完整翻译，功能、章节、数量、数值与条件须对应。未经要求不合并双语、不额外制作精简版，尤其不能因合并后的字节限制单方面删减。
9. **反馈是验收证据，不是全部测试的替代品。** 作者确认“视觉无误”只记录视觉通过；不能扩张为真实存档恢复、联机同步、其他模组兼容或性能提升。
10. **以必要、有效的验证完成交付。** 不用重复堆叠断言数量证明质量；通过后仅在代码又变、出现新失败或仍有风险时扩大测试。发现旧脚本针对过时列表、数值和资源，先修正覆盖范围再使用。

## 2. 版本选择和参考优先级

### 2.1 已验证过的环境快照

以下组合曾在制作过程中使用，仅供复现和识别接口，不代表最新版本或所有新模组必须固定使用：

| 项目 | 历史快照 |
| --- | --- |
| Godot／Godot.NET.Sdk | 4.5.1，使用 .NET／Mono 版编辑器 |
| 目标框架／语言 | `net9.0`／C# 12 |
| .NET SDK | 9.0.315 |
| 编译引用 | NuGet `STS2.RitsuLib` 0.6.2 |
| 核对过的游戏API | 0.111.0 |
| 当时工坊运行库 | RitsuLib 0.6.5 |

不要把以上组合写成“永远兼容”；`min_game_version` 是加载声明，也不能代替每个游戏版本的实测。新项目应记录实际游戏分支，再选择相应的 RitsuLib 主包或 Compat 包，避免浮动 `Version="*"` 带来不可复现的构建。

### 2.2 查接口的顺序

优先级是：**正在使用的游戏／依赖 DLL 与XML文档 → 对应版本源码或提交 → 当前项目的已验证实现 → 对应版本教程 → 其他示例**。网页教程用于指导思路；即使框架官网的示例，也须核对实际构造函数、方法签名和枚举。

旧反编译目录可能来自旧游戏。已遇到过同一个复制方法在旧源码无参数、实际 DLL 需要玩家参数的变化。不能仅凭旧 `.cs` 存在便认为新版本接口相同。必要时只反编译相关类型，不全量导出作为公开源码。

参考：[中文环境教程](https://tutorials.sts2modding.com/docs/01-env-setup/)、[RitsuLib入门](https://sts2-ritsulib.ritsukage.com/guide/getting-started)、[RitsuLib源码及版本](https://github.com/BAKAOLC/STS2-RitsuLib)。

## 3. 新项目结构、身份和依赖

建议结构如下；未用到的内容目录不用提前创建。

```text
ExampleMod/
├─ AGENTS.md 或项目约束文档
├─ ExampleMod.csproj
├─ ExampleMod.json
├─ project.godot
├─ export_presets.cfg
├─ local.props                 本机路径，忽略提交
├─ Scripts/
│  ├─ Entry.cs
│  ├─ Cards/
│  ├─ Powers/
│  ├─ Events/
│  ├─ Patches/
│  └─ UI/、RunData/             按需
├─ ExampleMod/                 模组独立资源前缀
│  ├─ images/cards/、powers/、events/、vfx/
│  ├─ scenes/
│  └─ localization/zhs/、eng/
├─ tools/                      可复用构建／验证工具
├─ tutorials/                  可选，只读学习材料
├─ docs/                       设计与发布说明
└─ .build/                     本地临时、测试和产物
```

项目根目录、Godot `res://` 根、模组资源前缀是三个概念。例如 `res://ExampleMod/images/cards/ExampleAttack.png` 对应项目内 `ExampleMod/images/cards/ExampleAttack.png`，不是磁盘项目根下的 `images/cards/`。

模组 ID、程序集名称、资源前缀、manifest 文件名、发布文件基名和命名空间应保持一致。资源大小写在 Windows 上可能被宽容处理，但迁移到其他平台不一定如此；统一文件名大小写。

首次先通过匹配的Godot .NET编辑器建立工程、创建C#解决方案，再调整脚手架。`project.godot` 中 `[dotnet]` 的 `project/assembly_name` 设为新程序集名称；不要只有目录结构和一个 `.csproj` 就认为Godot工程完整。

### 3.1 编译与运行依赖分开

- 游戏的 `sts2.dll` 和 `0Harmony.dll` 作为编译引用，通常设置 `Private=false`，不装进玩家 ZIP。
- NuGet `STS2.RitsuLib` 是编译依赖。游戏需要完整且兼容的 RitsuLib 运行安装；不能把 NuGet 缓存里的单个 DLL 当成完整运行包。
- manifest 的运行依赖 ID 是 `STS2-RitsuLib`，不是 NuGet 包名，也不是工坊的数字条目 ID。工坊“必需物品”另外按实际条目设置。
- 同一依赖通常只保留一份有效运行副本，避免本地 `mods/` 和工坊订阅同时注册同一个 ID。选择工坊副本前先确认它支持目标游戏与模组；不要删除编译所需 NuGet 缓存。
- 使用工坊运行库时显式配置 `RitsuLibAutoCopy=false`，检查引入模板的 build target，防止下次构建又自动复制依赖到本地游戏目录。
- 基础构建默认不部署游戏；需要安装测试时单独执行明确的复制步骤。

### 3.2 工程骨架

下例是上述历史API环境的起点，不是无需核对即可用于任意新版本的模板。路径从 `local.props` 或命令行传入，不写作者电脑路径。

```xml
<Project Sdk="Godot.NET.Sdk/4.5.1">
  <Import Project="local.props" Condition="Exists('local.props')" />
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>true</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>12.0</LangVersion>
    <AssemblyName>ExampleMod</AssemblyName>
    <RootNamespace>ExampleMod</RootNamespace>
    <Sts2DataDir Condition="'$(Sts2DataDir)' == ''">$(Sts2Dir)/data_sts2_windows_x86_64</Sts2DataDir>
    <RitsuLibAutoCopy>false</RitsuLibAutoCopy>
    <DeployToGame>false</DeployToGame>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="sts2">
      <HintPath>$(Sts2DataDir)/sts2.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>$(Sts2DataDir)/0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <PackageReference Include="STS2.RitsuLib" Version="0.6.2" />
    <Compile Remove=".build/**/*;sts2_decompiled/**/*" />
    <EmbeddedResource Remove=".build/**/*;sts2_decompiled/**/*" />
    <None Remove=".build/**/*;sts2_decompiled/**/*" />
  </ItemGroup>
</Project>
```

`DeployToGame` 只是约定属性；只有实际复制 target 检查这个条件，它才控制部署。检查所选模板是否有其他不受该属性控制的复制／发布步骤。平台数据目录也按实际安装修改。

新项目自行建立 `local.props`，将下例两项说明文字换成本机真实绝对路径；这两项不是有效路径的默认值：

```xml
<Project>
  <PropertyGroup>
    <Sts2Dir>游戏安装目录的绝对路径</Sts2Dir>
    <GodotExe>匹配的Godot.NET编辑器exe绝对路径</GodotExe>
  </PropertyGroup>
</Project>
```

如实际程序集目录不同，也在该文件配置 `Sts2DataDir`。`local.props` 不提交；可发布不含个人路径的模板和填写说明。

manifest 示例，游戏版本须替换成新项目真正支持的最低版本；不要把下例视为已为新模组验收：

```json
{
  "id": "ExampleMod",
  "name": "示例模组",
  "author": "作者",
  "description": "模组说明",
  "version": "0.1.0",
  "min_game_version": "0.111.0",
  "has_pck": true,
  "has_dll": true,
  "dependencies": [{ "id": "STS2-RitsuLib" }],
  "affects_gameplay": true
}
```

纯资源或纯代码模组按实际内容调整 `has_pck`／`has_dll`。旧游戏的 manifest 解析器及依赖格式另行核对。

可选起点：[RitsuLib工程模板](https://github.com/NothingFumo/RitsuLibModTemplate)。模板通常带示例角色、卡牌和占位资源；只保留新项目需要的部分，并核查自动部署及依赖版本设置。

## 4. 初始化、自动注册和稳定ID

历史环境中可用的最小入口：

```csharp
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace ExampleMod.Scripts;

[ModInitializer(nameof(Init))]
public static class Entry
{
    public const string ModId = "ExampleMod";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        // 按需注册本局数据、生命周期监听和补丁。
    }
}
```

`EnsureGodotScriptsRegistered` 处理附着在 `.tscn` 上的 C# 脚本类型；纯模型或补丁且不使用这种脚本的模组可省略。`RegisterModAssembly` 登记程序集归属，供自动注册发现模型注解；两者不能互相替代。

| 内容 | 已核对过的脚手架／注册方式 | 文本表 |
| --- | --- | --- |
| 卡牌 | `ModCardTemplate` + `[RegisterCard(typeof(...))]` | `cards.json` |
| 能力 | `ModPowerTemplate` + `[RegisterPower]` | `powers.json` |
| 遗物 | `ModRelicTemplate` + `[RegisterRelic(typeof(...))]` | `relics.json` |
| 药水 | `ModPotionTemplate` + `[RegisterPotion(typeof(...))]` | `potions.json` |
| 事件 | `ModEventTemplate` + 对应共享／幕事件注册 | `events.json` |
| 附魔 | `ModEnchantmentTemplate` + `[RegisterEnchantment]` | 按当前游戏表核对 |
| 充能球 | `ModOrbTemplate` + `[RegisterOrb]` | 按当前游戏表核对 |

新角色还需要角色模型、对应卡池／遗物池／药水池、起始卡组、角色选择与战斗资源；不能只注册几张卡便宣称角色制作完成。从角色脚手架与匹配版本模板起步。

注册注解来自 `STS2RitsuLib.Interop.AutoRegistration`，多数脚手架及资源 Profile 来自 `STS2RitsuLib.Scaffolding.Content`。有意集中注册时可使用内容包 API；不要为了同一个模型重复混用几套注册方法。

抽象卡牌基类需要 `Inherit=true` 才让注册信息作用于派生类。具体类在同一注册槽位重新声明会覆盖继承配置，不是自动进入第二个卡池。

默认公共 Entry 由模组ID、类别、类型名规范化生成，例如 `ExampleMod`／卡牌 `ExampleAttack` 通常对应 `EXAMPLE_MOD_CARD_EXAMPLE_ATTACK`。缩写、驼峰和特殊字符有专门处理，不是简单把完整字符串大写。核对运行时 `Id.Entry` 或该版本 `ModContentRegistry.GetFixedPublicEntry` 的结果，再写本地化。

已发布模型的 Entry 与存档身份有关。仅改显示名称通常不需要改 C# 类型或 ID；如果要改身份，必须考虑旧存档迁移和兼容，而不是仅改 JSON 键。

参考：[内容注册与内容包](https://sts2-ritsulib.ritsukage.com/guide/content-packs-and-registries)。

## 5. 卡牌制作流程和最小示例

### 5.1 先把效果写成可验证规则

每张牌明确：名称、费用、类型、稀有度、目标、所属池、图鉴可见性、关键词、基础数值、升级效果、生成入口、复制规则、特效及相关状态。

对复杂机制另外说明：

- “首次”按牌实体、战斗、玩家还是整局计数；真实复制和额外重放如何处理。
- “破格挡”“斩杀”“完全格挡”依据实际伤害结果，不依据预览或敌人初始格挡猜测。
- 百分比、取整、严格小于／小于等于，以及效果是在伤害前还是后生效。
- 层数是加算、乘算、持续回合还是触发次数；何时减层、何时清理。
- 关联卡牌绑定一张牌、一名玩家还是全部同名牌；跨堆、复制、消耗和返回手牌的行为。

优先找一张机制接近的原版牌，再参考当前项目实现。不要把旧模组的专用基类、静态状态或武器联动当成所有新卡牌必需。

### 5.2 普通攻击牌骨架

以下示例按历史0.111.0游戏API和RitsuLib0.6.2写法展示基础结构。它还需要第4章初始化、第7章本地化和实际卡图，不能仅粘贴类就视为完整卡牌。

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ExampleMod.Scripts.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ExampleAttack : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://ExampleMod/images/cards/ExampleAttack.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8m, ValueProp.Move)];

    public ExampleAttack()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target!)
            .Execute(context);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}
```

这张示例牌伤害8、升级增加3只是教程数字，不是平衡建议。普通奖励牌选择实际角色／无色池；衍生牌通常选择衍生池和相应稀有度，生成逻辑另做。

### 5.3 数值、预览和执行保持一致

- `CanonicalVars` 定义基础变量，升级改对应变量；卡面引用它，命令也使用同一数值来源。
- 普通攻击常将 `Damage.BaseValue` 交给原生命令，由它计算力量、易伤等修正。不要把已经修正的 `PreviewValue` 再送入同一修正链，造成重复加成。
- 区分 `ValueProp.Move`、不受增益影响与不可格挡等属性，按设计选择；普通攻击不要直接改敌人生命值绕过原生流程。
- 群体攻击使用 `TargetingAllOpponents` 等当前版本原生命令；`CardPlay.Target` 可能为空。伤后附加效果从真实攻击结果提取受击目标，按设计决定去重和死亡目标处理。
- 格挡使用原生格挡命令。条件格挡可用 `CalculatedBlockVar`；核对 `GainsBlock`、文本、预览和真实结算均识别同一条件，不能只给攻击类做共用联动。
- 图鉴、无主人牌、战斗外牌和目标预览可能没有 `Owner`／`CombatState`／`Pile`。动态计算按需求检查 `IsMutable` 及上下文，不在构造函数假设已有玩家。
- 运行时成长另保存累计量，再由基础值、成长值、当前修正统一计算，避免每次刷新都 `+=` 造成重复增长。降级重置原生变量时，是否保留成长由新玩法定义。

### 5.4 生成、复制和重放

战斗内生成使用当前版本的完整生成链，例如 `CombatState.CreateCard<T>(player)` 后通过 `CardPileCmd.AddGeneratedCardToCombat` 入堆。它包括设置归属、登记战斗牌集合和生成钩子；单独 `new T()` 不等于正确加入战斗。

真实复制与额外重放是不同对象：按实际API使用复制入口，并区分 `IsDupe`／`DupeOf` 等重放标志。自定义集合、状态对象和节点引用通常不会因浅复制自动获得正确语义；在克隆钩子中明确哪些复制、哪些共享、哪些重建。

每张实体牌的首次使用状态、跨回合回手牌、关联充能等状态不能使用一个无范围的全局布尔值。生命周期至少覆盖：生成、入堆／离堆、打出、复制、重放、移除、战斗结束和新局。

`[RegisterCharacterStarterCard]` 添加起始卡不等于“替换一张现有起始卡”。替换需求应精确找到相应起始卡组项，只替换指定数量，并验证新局、其他角色、重复初始化与不同角色模组组合。

参考：[动态变量工具](https://sts2-ritsulib.ritsukage.com/guide/card-dynamic-var-toolkit)、[中文内容教程入口](https://tutorials.sts2modding.com/docs/04-ritsulib/)。

## 6. 能力、生命周期和原版联动

### 6.1 能力与关键词分清职责

能力继承匹配版本的 `ModPowerTemplate`，明确 Buff／Debuff、计数／单一叠加类型、大小图标、静态描述和带层数的描述。用原版生命周期钩子在正确阶段触发，通过 `PowerCmd.Apply` 等命令施加／减层。

卡牌关键词注册只提供身份、展示位置与提示，不自动实现限制或战斗机制。真正效果放在牌、能力、监听器或必要补丁中。原生 `CardKeyword`／`CardTag` 属于游戏；RitsuLib命名空间提供扩展管理，不是替代原生枚举的全部定义。

若 Debuff 有额外阈值或来源快照，分别存储“持续层数”和“效果参数”，定义重复施加的合并规则；卡面、悬浮提示、能力实例和结算须显示／使用同一来源。

### 6.2 联动审查不是检查一张牌

扩展原版机制时搜索完整调用链：命令、能力、遗物、药水、生成牌、升级牌、自动打出，以及以具体原版类型硬筛选的逻辑。优先在公共入口或抽象机制层支持，避免每张相关原版牌复制一份效果。

修改伤害后验证多段、群体、自伤、间接伤害、格挡、减伤、死亡和战斗结束的次序。击杀房间最后一个敌人可能立即触发战斗结束门禁，使某些“伤后”钩子不再执行；应定位时机并去重，不直接移除全局战斗门禁。

提示只展示当前相关对象，避免一次创建整个池的预览。优先根据卡牌实际 Owner 查询状态，不能总取本地玩家；随机或战斗外无选择状态时使用清晰的回退。提示生成不可消耗战斗随机数、生成真实卡牌或更改任务状态。

### 6.3 选择补丁层级

优先原生生命周期与RitsuLib扩展点；缺少时使用精确目标补丁，再考虑IL改写。按功能建立 patcher，核对目标重载和返回类型。关键补丁未能应用时明确报错／停止相关功能；只有真正可选的兼容目标才忽略缺失。

常见命名空间：`STS2RitsuLib.Patching.Models` 提供目标／方法接口，`.Patching.Core` 提供注册扩展，`.Utils.HarmonyIl` 提供异步桥接辅助。具体接口随版本核对，不机械照搬教程片段。

### 6.4 异步补丁必须保持等待链

- 普通 Postfix 在原方法返回 Task 时执行，不代表原异步操作已经完成。要在完成后追加，替换返回 Task，让调用方仍能等待完整链并接收异常。
- RitsuLib `HarmonyAsyncTaskBridge.After` 用于这种完成后追加。`Before` 接收的原 Task 已经启动，不能保证追加动作早于原方法的同步首段。
- 必须替换／阻止原操作时，用正确 Prefix 或其他实际入口，返回匹配类型的 Task。不要使用 `async void` 让原调用方无法等待和观察失败。
- 某些短包装器可能被提前 JIT／内联，之后只补包装器会漏掉路径。先复现再决定是否使用 `PatchTarget.AsyncMethod`；它针对编译器状态机 `MoveNext`，不是普通返回Task的包装层。
- `MoveNext` 会在 await 恢复时反复调用。仅执行一次的逻辑必须区分初段和续执行；编译器字段、初始状态值及IL布局须检查目标DLL，不能把一套状态字段永久写死。
- 替换状态机若涉及 builder，必须完成原调用者正在等的那个 Task，并传播失败。若留下一条永不完成的等待链，表现可能是卡牌停在屏幕中央、房间不结束或不能继续。
- 异步等待后重新核对玩家、战斗／局身份和节点有效性，不能将旧场景的后续动作执行到新局。

这不是要求每个新模组都做状态机补丁；一般卡牌尽量使用原生命令与正常 `await` 即可。

参考：[Ritsu补丁指南](https://sts2-ritsulib.ritsukage.com/guide/patching-guide)、[Harmony文档](https://harmony.pardeike.net/)。

## 7. 本地化、卡面和提示

### 7.1 文件和动态格式

经验证的一种资源布局是 `res://ExampleMod/localization/zhs/cards.json`，英文对应 `eng/cards.json`。能力、事件、关键词等写入各自表。自定义UI可以使用RitsuLib的I18N或显式注册／加载自有表；不要假设任意新建JSON都会被自动发现。

```json
{
  "EXAMPLE_MOD_CARD_EXAMPLE_ATTACK.title": "示例攻击",
  "EXAMPLE_MOD_CARD_EXAMPLE_ATTACK.description": "造成{Damage:diff()}点伤害。"
}
```

对应英文表保留完全相同的键和变量：

```json
{
  "EXAMPLE_MOD_CARD_EXAMPLE_ATTACK.title": "Example Attack",
  "EXAMPLE_MOD_CARD_EXAMPLE_ATTACK.description": "Deal {Damage:diff()} damage."
}
```

- JSON不用注释或尾逗号，保存UTF-8；换行按JSON规则使用 `\n`。
- `diff()`、复数、条件分支及富文本使用目标版本实际 SmartFormatter 支持的语法，不自行发明占位符。
- 自定义布尔和计算字段通过对应描述上下文添加；覆写 `AddExtraArgsToDescription` 时保留基类处理。
- 卡牌提示、能力静态提示、实战能力提示和独立UI有不同上下文。某处可显示变量，不代表所有提示都自动获得它；逐个验证是否留下 `{Variable}` 原文。
- 关键词染色不一定创建提示。需要提示时注册关键词或通过 `AdditionalHoverTips`／提示工厂显式提供，且不重复添加原版已自动提供的提示。
- 中英文键集合、变量名与次数、条件和富文本标签对应；原版名词从目标游戏英文表核对，别凭印象造词。
- 翻译发现中文、代码或旧说明矛盾时指出差异，再按已确认机制同步；不要悄悄让中英文变成两套规则。

参考：[本地化与关键词](https://sts2-ritsulib.ritsukage.com/guide/localization-and-keywords)。

### 7.2 卡框和缺失资源

未制作卡框时只配置真实存在的卡图，让框架使用默认卡框。自定义卡框、横幅、材质等须在正式PCK中真实加载成功后启用；卡框并不只是画一张漂亮矩形，还涉及透明区域、类型差异、标题、能量与文字区域。

重复资源警告应检查路径、大小写、导入映射和配置，解决缺失原因，不屏蔽日志。回退成功不等于完全没有开销；未经帧时间采样不能报告具体FPS下降或提升。

## 8. 事件、多级选项和流程插入

普通事件从 `ModEventTemplate` 和相应注册方式起步。`[RegisterSharedEvent]` 的注册类别不能直接等同多人共享行为，后者看事件实际 `IsShared` 和当前游戏逻辑。

- 初页用 `GenerateInitialOptions`，后续页用 `SetEventState(description, options)`；结束用 `SetEventFinished`，让原生清理及结束状态完成。
- 按实际 Entry 组织 `{ENTRY}.pages.{PAGE}.description` 与 `{ENTRY}.pages.{PAGE}.options.{OPTION}.title/description`；使用 `InitialOptionKey`、`ModOptionKey` 等辅助，避免手写键与回调不一致。
- 翻页／返回只是导航，不提前发奖、确定随机结果或污染恢复历史。适用时用 `ThatWontSaveToChoiceHistory()`。
- 在未经改造的原版选项布局中，经验上按每页最多四个按钮安排，包含返回与翻页；自定义布局可以不同，但必须实际检查最小窗口与长英文选项。
- 图片路径由资源 Profile 和当前页切换逻辑共同决定，不能只放文件而忘了页面引用。

**注册成功不等于会在指定时机进入。** 普通事件池注册只提供抽取资格；开局或指定流程后的强制事件还需接入真实房间／事件结束链。仅供手动进入的事件也需注册，以便 `ModelDb` 与读档查找，但可用 `IsAllowed=false` 排除普通随机抽取。

前置事件结束时不要在尚未返回的选项回调内立即切房间后又等待该回调本身。必要时先让回调返回／异步让步，再等待其他选项及保存任务。记录“已排队／正在进入／已完成”等本局门禁，异步恢复后重查局身份，避免重入和旧局任务污染。

多人需确认各玩家完成条件、共享或独立事件模型、本地画面所有者、Proceed／地图打开时机与新房间保存。读档不能把一名玩家完成误当全员完成。

其他模组也改开局时，优先链式等待已有流程或用统一扩展点，不强行覆盖房间。未安装对方模组、未核对源码和实机时，只能说“预留兼容”或“已调查”，不能说“兼容通过”。

参考：[中文事件教程](https://tutorials.sts2modding.com/docs/04-ritsulib/04-12-add-event/)、[Ritsu自定义事件](https://sts2-ritsulib.ritsukage.com/guide/custom-events)。

## 9. 原生独立UI、顶栏和HUD安全区

### 9.1 界面组织与外观

先明确入口、导航、状态、详情与操作，再制作原生界面。HTML／设计技能适合比较方向和交互，不能替代Godot中的字体、层级、命中与窗口缩放验收。

默认使用实际游戏当前语言的字体和主题。已有实现使用过 `FontManager.GetSubstituteFont(language, FontType.Regular)`；签名按当前游戏核对。文字深色描边、原版横栏／槽位边框可帮助贴近游戏；不把某个中文字体名、字号或青灰配色固定成所有项目标准。

采用外部字体前先检查中文与英文覆盖，有些所谓主题字体其实只有符号，没有汉字。不能仅凭文件名认定可用。

图谱主画面使用短节点名和清晰状态，条件、奖励、进度放在点击后的详情。给后续支线留空间；连接线表达真实前置关系，独立支线不要画成依赖主线。长详情可滚动，但关闭、领取等操作要可达。开发用重置演示、模式切换、设计预览字样不进入玩家界面。

### 9.2 接入原生屏幕流程

顶栏可使用 `IModTopBarButtonHandler` 和 `[RegisterOwnedTopBarButton(...)]`，由当前版本注册与屏幕服务打开实现 `ICapstoneScreen` 的Godot `Control`。

- `IsVisible`／`IsOpen`／`GetCount` 可能每帧执行，保持廉价；昂贵查询按数据变化缓存，不在每帧遍历所有卡牌或创建纹理。
- 初始化前、退出后或两局之间，context中的玩家可能为空，必须有安全回退。
- 原生容器可能不存在；打开失败时处理未挂载的临时节点，不遗留实例和订阅。
- 打开页面登记游戏热键阻挡；关闭与退出树均解除，订阅也解除。关闭尽量经过原生容器，回调幂等，节点释放策略与所有权一致。
- Esc／返回先关闭详情，再关闭主页面；标记输入已处理。设置默认焦点和手柄导航，检查拖动、缩放和鼠标释放。
- 原生屏幕容器在已核对版本只对单人／假联机暂停战斗，不能默认宣布多人打开页面会暂停。

### 9.3 避免顶栏和遗物遮挡

“全屏”不等于自动盖住／避开原版HUD。先决定页面是否保留HUD，再计算布局和输入。

保留HUD时，测量实际可见顶栏背景及遗物条目占用，将四角通过画布变换转换到页面局部坐标，再计算剩余安全区。不要把覆盖整屏的遗物父容器空白外框误算成占用，也不要只用固定顶部像素值。

窗口、UI父变换、遗物换行和显隐都会改变安全区。页面打开期间按需要检测真实占用变化；只监听页面自身 `Resized` 会漏掉遗物换行。等比缩放后居中，不强行设置会裁切的小窗口最小倍率。

测试必须把页面与真实HUD放在同一场景：长遗物栏、多排遗物、窗口缩放、UI变换、标题、关闭／领取按钮鼠标命中、键盘与手柄。只看独立页面截图不能证明游戏中不遮挡。

参考：[顶栏按钮教程](https://tutorials.sts2modding.com/docs/04-ritsulib/04-21-top-bar-button/)。

## 10. 状态范围、存档与联机

### 10.1 先确定保存范围

| 数据性质 | 合适范围 |
| --- | --- |
| 一张牌本场首次使用、临时动画索引 | 实体牌／战斗，不自动跨局保存 |
| 本局选择、任务、累计奖励 | 玩家本局数据 |
| 真正跨局解锁／永久成长 | Profile或专用永久存储，另行设计迁移 |
| 音量、显示或玩法配置 | 设置系统，不当本局进度仓库 |

过往有本局任务，不代表新模组只能做本局系统。按新需求决定范围，不能用静态字典假装永久存档，也不能把本局进度写进全局设置。

### 10.2 Ritsu本局数据

核对过的API包括 `GetRunSavedDataStore(ModId)`、`RegisterPerPlayer`、`PlayerRunSavedData<T>`、`RunSavedDataOptions` 与 `BeginModDataRegistration`。注册数据结构要有默认值、SchemaVersion和必要迁移；字段缺失默认值不能解决所有破坏性类型变化。

特别注意：`WritePolicy.WhenSet` 只写出经API标记变更的值。`Get` 返回对象可被修改，不等于已标记待保存；使用合适的 `Set`／`Modify`。注册槽位和修改内存也不等于已写盘，最终仍需经过原生保存时机或当前版本合法的保存入口。

进度用真实战斗胜利等生命周期统计；订阅可参考 `SubscribeLifecycle<CombatVictoryEvent>`，按实际房间和玩家筛选。回调、读档、重入须去重，不能从UI刷新次数推导任务进度。

至少验证新局清零、同局恢复、两玩家隔离、缺失字段、重复领取和版本迁移。真正跨局系统另外验证用户档案、退出重启及迁移。

### 10.3 联机不是“能发消息”

RitsuLib Sidecar提供消息描述符、序列化、处理、可靠传输和路由等能力；按当前DLL核对接口。请求只传业务目标／参数，从可信传输的发送者解析玩家，不能相信载荷自报的身份。

接收端再次验证：当前局、玩家资格、目标枚举／范围、阶段、完成条件、重复请求与消息长度。按钮禁用不替代接收验证。

`SendToHostAndBroadcast` 的发送成功不能当成业务成功、奖励已领取或存档已写盘；已核对实现还有先转发后处理的路径，不能宣传它自动提供“主机业务验证成功才广播”。按需求设计一致的各端状态转移，或独立的权威结果／确认同步。

存档API不会自动同步每一次运行时变更。夹具中的两玩家隔离、载荷往返和消息处理测试，也不等于真正两客户端联机通过。

参考：[Ritsu持久化指南](https://sts2-ritsulib.ritsukage.com/guide/persistence-guide)、[中文数据保存教程](https://tutorials.sts2modding.com/docs/04-ritsulib/04-17-data-save/)。

## 11. 美术、VFX和TSCN的通用制作规范

### 11.1 一组资源先做一致，再批量生产

不同用途允许不同表现，但共享主体身份：卡图负责场景与氛围；透明素材负责完整轮廓和运动；图标负责小尺寸识别。关键几何结构、部件连接、比例和配色必须对应，不能卡图是一种形状、悬浮是另一种。

使用同一代表对象比较候选方向，展示卡面裁切、透明底和小尺寸图标。候选数量按决策需要，不能把某轮“四个方案”当成每次必须产出的数量。

审核检查：部件数量、左右方向、连接与透视、形状常识、裁切和留边、透明孔洞、背景光晕、缩小辨识、与其他正式素材的统一。判断尺寸看可见主体边界，不只看透明画布宽高；看alpha，不能凭棋盘底或黑底截图猜是否真的透明。

记录原稿、选稿依据与采用版本。确认后只替换真正被加载的正式路径，重新导入并检查最终PCK；原稿／试稿不混进玩家包。

图像生成技能可用于插画、透明贴图与图标，设计技能可用于UI样稿；具体可用工具按新环境决定。它们不是新的模组运行依赖，也不能代替原生审图和资源验证。

### 11.2 表现变换优先在实际场景修正

先查代码加载的是哪个 `.tscn`，再修改相关 Sprite／Visual节点。左右镜像可用 `flip_h`，方向旋转用节点 `rotation`；单位为弧度，顺时针90度约 `1.5707963`。随后核对偏移、锚点、命中框和粒子方向。

方向错误一般不需要重绘或旋转PNG，更不需要重写整个轨道。已做好且合适的原版／现有场景运动应优先复用；旧场景仅作参考时也要标明，避免编辑后实际游戏没变化。

将轨道节点、静止素材变换和攻击运动分开。调整外观尺寸时核对Sprite、粒子和命中范围；放大主体不等于放大轨道。静止方向与动画复位值保持一致。

### 11.3 持续VFX生命周期

- 挂载和所有权跟随正确角色／卡实体；全局高 `ZIndex` 不应让战斗特效穿过地图或全局UI。
- 根据人物前后位置使用合适遮挡／排序；轨道的形状、中心、速度和倍率由新项目或原版参考决定，不能把旧武器参数硬搬。
- 每次攻击前终止旧Tween并回到固定局部静止坐标，避免重放、中断后把敌人坐标当成新原点。
- 攻击完成、异常、中断、节点退出都有复位／清理路径，不能让后续出牌永久等待纯表现任务。
- 开地图时按需求隐藏持续战斗VFX及提示，关闭恢复；战斗结束主动清理，退出树兜底解除事件、Tween、Hitbox和活动索引。
- 对生成、复制、多段、群体、重放、消耗、死亡、战斗结束和返回地图分别观察；检查是否多生成一份、失去归属或留在中央。

有自定义C#节点的场景，还要确认程序集脚本注册与PCK中的Script资源绑定，见第12章。

## 12. 从干净工程构建、导入和打包

### 12.1 正确顺序

1. 核对工程身份、目标版本、游戏DLL路径、兼容SDK及RitsuLib包，恢复NuGet依赖。
2. 用匹配的Godot在项目根导入资源，生成 `.godot/imported/`，检查失败。如果首次导入需要已编译C#类型，可以先构建一次，再导入。
3. 按明确配置完成最终C#构建，不复制到游戏、不自动部署运行依赖。
4. 为本次产物建立独立staging目录，收集最终DLL、manifest和新生成的PCK；防止旧PCK混入。
5. 从最终DLL／PCK验证脚本类型、资源和文本，再做三文件ZIP及哈希记录。

使用标准导出前，先安装匹配导出模板，并在Godot创建名为 `Windows Desktop` 的导出预设。选择本模组自有资源，包括仅按代码路径动态加载的素材／场景；在非资源包含过滤中显式加入本地化JSON，例如 `ExampleMod/localization/*/*.json`。排除教程、试稿、测试和原版解包参考，不能把 `.gitignore` 当Godot导出过滤器。预设名称和命令须相同，`--export-pack` 不会替你创建预设。参见[Godot导出配置](https://docs.godotengine.org/en/4.5/tutorials/export/exporting_projects.html)。

下例在新项目根目录执行，先完成上述配置；它是操作示例，不是覆盖全部发布工作的脚本：

```powershell
$ErrorActionPreference = 'Stop'
$taskProjectRoot = (Get-Location).Path
$taskProjectFile = Join-Path $taskProjectRoot 'ExampleMod.csproj'
$taskLocalPropsPath = Join-Path $taskProjectRoot 'local.props'
[xml]$taskLocalProps = Get-Content -LiteralPath $taskLocalPropsPath -Raw
$taskGodotExe = [string]$taskLocalProps.Project.PropertyGroup.GodotExe
if (-not (Test-Path -LiteralPath $taskGodotExe)) { throw '请填写有效的Godot.NET编辑器路径。' }
$taskPackageRoot = Join-Path $taskProjectRoot '.build/package/0.1.0/ExampleMod'
New-Item -ItemType Directory -Path $taskPackageRoot -Force | Out-Null
$taskPckPath = Join-Path $taskPackageRoot 'ExampleMod.pck'

dotnet restore $taskProjectFile '-p:Configuration=ExportRelease' '-p:RitsuLibAutoCopy=false'
if ($LASTEXITCODE -ne 0) { throw 'NuGet恢复失败。' }
& $taskGodotExe --headless --editor --path $taskProjectRoot --import
if ($LASTEXITCODE -ne 0) { throw 'Godot导入失败。' }
dotnet build $taskProjectFile -c ExportRelease --no-restore '-p:RitsuLibAutoCopy=false' '-p:DeployToGame=false'
if ($LASTEXITCODE -ne 0) { throw 'C#构建失败。' }
& $taskGodotExe --headless --path $taskProjectRoot --export-pack 'Windows Desktop' $taskPckPath
if ($LASTEXITCODE -ne 0) { throw 'PCK导出失败。' }
# 核对最终TargetPath，把该配置的DLL和manifest复制到taskPackageRoot，再执行资源与ZIP验证。
```

Godot4.5.1 SDK有 `Debug`、`ExportDebug`、`ExportRelease` 配置；不要无检查地把普通 `.NET -c Release` 当成它的导出配置。`ExportRelease` 会涉及优化，必须在该配置下验证关键补丁。输出路径由实际配置决定，不固定从 `Debug` 目录取DLL。

`--no-restore` 只能用于已完成、配置相符的恢复之后，示例因此明确以 `Configuration=ExportRelease` 恢复。Godot导入／导出过程若触发其他配置或重新构建，应按最终目标确认对应依赖恢复和实际输出。依赖旧 `.godot/mono/temp/obj`、SDK缓存和导入产物的快捷脚本不算干净工程引导；“之前电脑可以构建”不能证明新电脑也可复现。

以上命令不是本次替用户执行的动作，也不保证匹配所有未来SDK。标准导出可用时优先采用；遇到C#导出插件限制再考虑下面的有边界打包器。

参考：[Godot命令行与导出](https://docs.godotengine.org/en/4.5/tutorials/editor/command_line_tutorial.html)。

### 12.2 自定义PCK打包器的必要条件

用原生GDScript的 `PCKPacker` 可以避免无关C#导出步骤，但须自己管理依赖清单：

- 收集模组自己的资源前缀，例如 `res://ExampleMod/`。
- 读取模组素材 `.import` 的映射，补入真正引用的 `res://.godot/imported/` 导入产物，不只包原始PNG。
- 场景挂载本模组C#脚本时，确认对应 `res://Scripts/...cs` Script资源可解析。DLL有类型不代表场景资源绑定自动齐全；必要的Script／UID按实际加载方式包含并验证。
- 检查场景、材质、GDScript、字体等其他自有外部依赖，不能把“递归扫描一个目录”当成任意依赖自动解析器。
- 引用原游戏资源时，在游戏自身PCK中解析；不把整套原版图片、主题、字体、动画和原版DLL塞进模组包。独立夹具需要这些资源时先加载游戏包，再加载模组包。
- 保留稳定UID和路径，但不要把另一个项目的缓存／UID身份整套复制成新项目。实际实例化和绑定结果比“文件名存在”更重要。

### 12.3 玩家安装包

含代码与资源的普通模组交付结构：

```text
ExampleMod-0.1.0.zip
└─ ExampleMod/
   ├─ ExampleMod.dll
   ├─ ExampleMod.pck
   └─ ExampleMod.json
```

不混入测试、源码、封面、原图、发布文案、NuGet运行依赖或原游戏DLL。只代码／只资源模组按manifest实际声明调整；不要机械为不存在的产物造文件。

ZIP列目录确认结构和文件集合，再逐文件比较SHA256与staging，记录整个ZIP的哈希。记录实际使用的构建配置、游戏与依赖版本，不因产物放在GitHub Release便称它采用了Release编译配置。

## 13. 验收方式与问题定位

### 13.1 分层验收

| 层级 | 至少检查 | 不自动证明 |
| --- | --- | --- |
| 编译 | 退出码、错误、警告、目标配置 | 游戏行为正确 |
| 内容／资源 | 最终PCK路径、纹理解码、场景实例化、Script绑定、JSON和键 | 图像结构、美术品质 |
| 文本 | 真实格式化、变量、条件、复数、静态／实战提示 | 英文长文本实际排版 |
| 行为夹具 | 真实模型与命令、边界数值、复制／重放、最后击杀 | 所有房间UI流程 |
| 原生UI | 实际字体／HUD叠加、缩放、层级、鼠标与热键 | 实机联机或保存写盘 |
| 作者实机 | 按具体反馈记录通过范围 | 未明确测过的其他范围 |
| 保存／联机／兼容 | 分别专项验证 | 不能由前几层合并推定 |

夹具加载最终生产DLL／PCK，核对SHA256，必要时核对MVID，避免测到旧二进制。动态发现当前内容或明确维护清单；不要复制旧脚本的固定卡牌数量、图标列表和过时描述当成通用通过标准。

复用场景或资源库时，资源警告、原生进程退出码、程序集加载和最终断言分别检查。原生打包进程不需要加载C#，不代表验证自定义C#节点的进程也能忽略程序集加载失败。

### 13.2 常见症状定位表

| 症状 | 优先检查 |
| --- | --- |
| 注册了内容但游戏找不到 | 初始化是否执行、程序集归属、注册冻结时机、模型ID、卡池／条件 |
| 卡面留下 `{Var}` | 名称大小写、动态变量与描述上下文、静态和实战提示差异 |
| 资源警告不断重复 | 真正加载路径、大小写、缺失卡框／图标、导入映射、最终PCK |
| 同样的卡偶尔绕过补丁 | 真实公共入口、重载、JIT提前编译／内联、异步首段与续执行 |
| 牌卡在中央／不能继续 | Task是否完成、异常是否传递、状态机builder、纯VFX等待和动画复位 |
| 最后一次击杀收益丢失 | 战斗结束门禁、伤后钩子顺序、实际结果、去重 |
| 地图上仍有战斗特效 | 全局图层、所有权、地图显隐、战后清理和退出树兜底 |
| 独立UI正常但进游戏遮挡 | 顶栏／遗物真实占用、坐标变换、换行、窗口大小、点击命中 |
| 存档字段有值但重进丢失 | 写入策略、Set／Modify标记、真实保存时机、Schema／迁移 |
| 客户端按钮显示成功但没收益 | 请求与业务确认、发送者归属、各端状态转移、保存与同步分工 |
| 更新后不生效 | 运行副本重复、旧DLL／PCK、manifest版本、订阅下载状态、实际哈希 |

性能结论需要帧时间、资源数量或其他相关测量；日志减少、加载通过或CPU看起来不高，均不能单独证明FPS改善。

## 14. Git、工坊与GitHub发行

### 14.1 Git忽略边界

保留源码、工程配置、manifest、正式素材、本地化、作者资源旁 `.import`／`.uid`、可复用构建工具和正式发布说明。忽略生成缓存、临时包、个人路径配置、未采用试稿和本地大体积参考。

基础 `.gitignore` 示例：

```gitignore
/.godot/
/.build/
/bin/
/obj/
/.vs/
/.vscode/
/local.props
/design-demos/
*.user
*.suo
*.pck
*.dll
*.pdb
*.zip
*.tmp
*.log
```

只有确实在根目录放了原版解包参考时，才另外忽略根 `/images/`、`/scenes/`、`/fonts/` 等；若新项目的正式资源就放在那里，则不能照搬此规则。不要全局忽略 `.import`、`.uid`、`.tscn` 或所有图片。

用 `git check-ignore --no-index` 验证正式文件未被排除，检查 `git ls-files -ci --exclude-standard`。`.gitignore`不影响已经跟踪的文件；确需停止跟踪时只对明确目标用 `git rm --cached`，保留本地文件，不做全仓重置。参见[Git官方说明](https://git-scm.com/docs/gitignore)。

### 14.2 发行内容和语言

- GitHub提交当前源码并推送后，给对应提交打版本tag，附上真正的玩家安装包和更新日志。没有源码快照和回退用途的中间开发版不必逐个补发。
- GitHub自动源码压缩包是源码快照，不是已构建的模组安装包。编译包放Release附件，不进入源码历史。参见[GitHub Release说明](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)。
- 工坊更新现有条目要沿用其发布ID和工作区；新模组必须建立自己的身份，不复制另一模组的数字条目ID、`mod_id.txt`或更新目标。
- 工坊content目录放对应版本的运行文件；封面、上传器配置与文案在上传工作区外层，不进玩家安装目录。具体上传命令以当前游戏发布工具／模板指南为准。
- 高清封面原稿与上传图分开保留。Steam预览图要求小于1MB，导出后实际检查大小和画面；768×768只是可选上传尺寸，不是固定规范。[Steam预览限制](https://partner.steamgames.com/doc/api/ISteamUGC#SetItemPreview)
- 中英文简介、日志分别提供，逐项对应。Steam简介按单语言UTF-8字节数核对8000字节上限，不把两种语言合并后压缩。[Steam描述常量](https://partner.steamgames.com/doc/api/ISteamRemoteStorage#Constants)
- 文案只写已实现内容。AI辅助内容如使用，应据实说明筛选、修改和整合，不继承另一个模组的宣传标题、彩蛋或未实现路线。
- 上传后核对条目、公开状态、依赖和订阅下载的版本；用订阅运行副本复验，避免本地开发副本干扰。

## 15. 参考索引与技能用法

以下链接是独立参考入口，不需要迁移原项目文件。在线文档核查于2026-10-09；实际实现仍服从第2章的版本核对原则。

| 用途 | 参考 |
| --- | --- |
| 工程搭建、游戏程序集与基础导出 | [中文环境配置](https://tutorials.sts2modding.com/docs/01-env-setup/) |
| 中文各类内容制作、按章节保存本地教程 | [RitsuLib中文教程目录](https://tutorials.sts2modding.com/docs/04-ritsulib/) |
| 框架初始化、主包／Compat分支 | [Ritsu入门](https://sts2-ritsulib.ritsukage.com/guide/getting-started) |
| 框架实现、XML文档、发行版本和对应提交 | [STS2-RitsuLib源码](https://github.com/BAKAOLC/STS2-RitsuLib) |
| 注册、内容包、模型身份 | [内容注册](https://sts2-ritsulib.ritsukage.com/guide/content-packs-and-registries) |
| 卡牌动态数值和描述 | [动态变量](https://sts2-ritsulib.ritsukage.com/guide/card-dynamic-var-toolkit) |
| 提示、关键词、多语言 | [本地化与关键词](https://sts2-ritsulib.ritsukage.com/guide/localization-and-keywords) |
| 事件与多页状态 | [自定义事件](https://sts2-ritsulib.ritsukage.com/guide/custom-events)、[中文事件教程](https://tutorials.sts2modding.com/docs/04-ritsulib/04-12-add-event/) |
| 补丁与异步入口 | [Ritsu补丁](https://sts2-ritsulib.ritsukage.com/guide/patching-guide)、[Harmony](https://harmony.pardeike.net/) |
| 保存策略、隔离和迁移 | [持久化](https://sts2-ritsulib.ritsukage.com/guide/persistence-guide)、[中文保存教程](https://tutorials.sts2modding.com/docs/04-ritsulib/04-17-data-save/) |
| 原生顶栏入口 | [中文顶栏教程](https://tutorials.sts2modding.com/docs/04-ritsulib/04-21-top-bar-button/) |
| 可替换身份的工程脚手架 | [RitsuLibModTemplate](https://github.com/NothingFumo/RitsuLibModTemplate) |
| 原生导入、命令行打包 | [Godot4.5命令行](https://docs.godotengine.org/en/4.5/tutorials/editor/command_line_tutorial.html) |

如需要离线教程，可从对应来源整理到新项目的 `tutorials/`，按用途保存 `card.txt`、`power.txt`、`event.txt`、`Keyword.txt`、`relic.txt`、`potion.txt`、`PatchSystem.txt`、`lifecycleevent.txt`、`dataingame.txt` 等。它们是学习资料，不是运行资源；没有这些本地文件时查对应网页，不应因旧目录缺失而中断已能继续的开发。

本文件已吸收旧 `card-generator` 的通用流程，不需要搬它的项目专用路径和逐步审批。若新环境有 `imagegen`、`huashu-design`、`skill-creator` 等技能，按需要辅助美术、UI方向和技能维护；不强制新项目安装同一套插件，更不把某个HTML预览当成原生实现。

文档中的接口示例基于核对过的环境，**本文件的文档／技能格式检查不等于已在一个新模组中编译运行这些示例**。迁移后先完成最小加载闭环，再逐步扩展内容。

## 16. 新项目首轮交付模板

可将下面一段作为新项目第一轮请求，填入实际值：

```text
请先读取本项目AGENTS.md或sts2-ritsulib-mod-development技能。
目标：为[内容或角色]制作[本轮明确功能]。
模组ID：[新项目ID]。
目标游戏／Godot／.NET／RitsuLib版本：[实际版本]。
约束：基于STS2-RitsuLib，不新增第三方运行库；仅修改本项目目录。
输出：先给计划，再实现；逐条说明改动和验证范围。
内容：明确卡牌字段、触发时机、状态范围、复制／重放与升级规则。
素材：卡图／图标／透明素材／场景按功能需要制作，采用前逐张审图。
语言：中文、英文分别维护，内容与规则逐项对应。
验收：编译通过；提供对应游戏内可执行的验证步骤；安装包与源码一致。
部署或上传：[是否已授权及明确目标；未授权则只在项目内生成结果]。
```

首轮先交付一张最小牌或一项最小功能，确认注册、文本、资源、构建和游戏加载整条链路；之后增加复杂机制。不要把旧模组完整搬来再逐个删专用玩法。
