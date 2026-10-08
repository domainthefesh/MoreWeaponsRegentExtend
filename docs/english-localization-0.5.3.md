# 0.5.3 英文本地化

本轮新增 `MoreWeaponsRegentExtend/localization/eng/` 四张完整英文表。英文按现有实现翻译，不改变卡牌 ID、存档 ID、武器选择、数值或规则。

## 覆盖范围

| 表 | 键数 | 内容 |
| --- | ---: | --- |
| `cards.json` | 40 | 16 件主武器、3 张附属牌、1 张初始牌的名称与描述 |
| `powers.json` | 19 | 8 种能力，以及重压、眩晕、适应的实时描述 |
| `card_keywords.json` | 16 | 8 个自定义关键词的名称与解释 |
| `events.json` | 78 | 兵器库母页、四类武器、16 件武器选择、随机模式、完成页与分页导航 |
| **合计** | **153** | 与中文表的键集合完全一致 |

所有动态变量、`diff()`、`choose(...)`、`cond` 分支及 `[gold]`、`[purple]`、`[red][b]` 标签均保留。中文三个架势的分支顺序、炮号为 0 时的分支、Parry 未激活时隐藏格挡的分支保持不变。晶刃的 `Repeat`、圣棍横扫的 `Hits` 使用原版君王之剑的 `plural:| {} times` 语法：一次攻击不显示冗余次数，重复攻击显示正确的英文复数。

## 统一名称

| 中文名称 | 英文名称 |
| --- | --- |
| 君王重锤 | Sovereign Warhammer |
| 邪皇圣弓 | Dread Emperor's Sacred Bow |
| 君王晶刃 | Sovereign Crystal Blade |
| 君王之盾 | Sovereign Shield |
| 君王长矛 | Sovereign Spear |
| 君王重斧 | Sovereign Greataxe |
| 皇室猎枪 | Royal Rifle |
| 君王镰刀 | Sovereign Scythe |
| 海王三叉戟 | Neptune's Trident |
| 天皇太刀 | Imperial Katana |
| 君王之翼 | Sovereign Wings |
| 君王短匕／君王短匕首 | Sovereign Dagger |
| 定海圣棍 | Sea Calming Staff |
| 银河轨迹炮 | Galactic Trajectory Cannon |
| 光之圣典 | Codex of Light |
| 皇家指虎 | Royal Knuckles |
| 趁手兵器 | Handy Weapon |
| 水晶小刀 | Crystal Dagger |
| 子弹 | Bullet |
| 充能 | Charge |

“架势”统一为 **Stance**，三个姿态是 **Light Swing / Sweep / Heavy Chop**；重压为 **Heavy Pressure**；居高临下为 **High Ground**；赐福为 **Blessing**；武痴为 **Martial Fanatic**；适应为 **Adaptation**；热寂灭／热启动为 **Heat Death / Hot Start**。能力“王之枪”使用与卡牌一致的 **Royal Rifle**，避免把一把枪写成两个不相干的英文名称。

模组发布页面可考虑使用英文名 **Sovereign Arsenal**，副标题 **Regent Weapon Expansion**。这只是发布命名建议，未改 manifest 名称或模组 ID。

## 原版术语来源

从本机游戏 `D:/steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.pck` 的 `localization/eng/cards.json`、`powers.json` 和 `card_keywords.json` 只读核对原版名称，确认了 **Sovereign Blade、Seeking Edge、Lightning Rod、Radiance、Regen、Curious、Demise、Imbalanced、Strangle**。沿用 **Forge、Block、Retain、Exhaust、Doom、Fatal、Strength、Dexterity、Vigor、Barricade、Vulnerable、Weak、Poison**。

赐福的正面效果使用原版英文能力名称，特别是 `CuriousPower` 的名称是 **Curious**，`RegenPower` 的名称是 **Regen**，未自行改成 Curiosity 或 Regeneration。

## 现有中文表述与实现的差异

以下只是本轮翻译核对发现的差异，未修改中文或机制：

1. 皇室猎枪中文描述的“铸造 5”实际由 `SovereignGun.OnPlay` 给予 **5 层 KingGunPower**，并不调用 `ForgeCmd.Forge(5)`。英文据实写为 **Gain 5 Royal Rifle**，把实际的后续 Forge 增加层数／生成子弹独立写出。
2. 王之枪中文写“选择消耗一张攻击牌”，但 `KingGunPower.AfterPlayerTurnStart` 向 `CardSelectCmd.FromHand` 传入的筛选参数为 `null`，实际可选择任意手牌。英文写为消耗一张手牌；有伤害变量时计算该牌伤害，然后加上该能力的层数，目标是随机敌人。此差异并未以本地化工作为由改动选牌规则。
3. 兵器库的“君王短匕首”和卡面的“君王短匕”是同一 `SovereignDagger`。英文卡面、选项、完成页统一为 **Sovereign Dagger**。
4. 光之圣典显示的 `{Blessings}` 已含基础 1 与累计铸造奖励；英文的“每 10 Forge 额外 1 Blessing”是该总数的计算说明，不在打出时再额外执行一次。
5. 圣弓的 `{DebuffCount}` 已含基础 1 与每 15 累计铸造增加的次数；英文同时显示当前次数和增长条件。
6. 重压英文区分 **on application** 的阈值快照与后续重新施加的较高阈值。每段伤害在伤害修正后、格挡扣除前比较；“below”是严格小于，等于阈值仍正常结算。

## 本轮静态检查

已用 PowerShell / .NET 自带 JSON 与正则检查，不新增第三方依赖：

- 四个 JSON 均能完整解析，153 项没有空缺或额外键。
- 中英文每一项的动态变量名称及出现次数相同。
- 中英文每一项的富文本标签及顺序相同。
- 英文表没有残留中文字符。

最终生产 DLL 的 489 项专项断言通过：实际 `GetDescriptionForPile`（包含生产 `AddExtraArgsToDescription`）、动态预览和原版 SmartFormatter，8 能力 dumb/smart 提示、16 关键词、16 武器实际事件回调及三种模式；没有裸变量或中文残留。攻击次数根据验证反馈改为原版 `plural` 格式，单次省略次数，复数正常显示。英文来源与最终 PCK 同步核对，详细证据见 [0.5.3 更新记录](update-0.5.3.md)。

这是实际文本格式化和资源加载验证，尚未驱动英文卡面 UI。长名称、长提示、分页按钮的实际布局仍待作者实机检查，不能据此声称游戏内排版已验收。
