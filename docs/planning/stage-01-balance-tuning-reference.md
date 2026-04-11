# 第一阶段数值调整参考

最后更新：2026-04-11

## 文档用途

这份文档用于说明第一阶段原型中：
- 英雄基础属性在哪里修改
- 技能数值在哪里修改
- 技能倍率如何换算成实际伤害
- 如何通过战斗日志核对技能是否按预期生效

这份文档面向实际调数，不面向总体规划。

## 当前数值资源位置

第一阶段演示内容主要使用以下资源目录：

- 英雄配置：`game/Assets/Data/Stage01Demo/Heroes/<heroId>/`
- 技能配置：`game/Assets/Data/Stage01Demo/Skills/<heroId>/`

推荐优先在 Unity Inspector 中直接修改这些 ScriptableObject 资源，不建议日常手改 `.asset` 文本。

### 与 Demo 菜单相关的注意事项

当前项目里有三类常用菜单入口：

- `Fight/Stage 01/Generate Demo Content`
  默认安全模式，只会补齐缺失的 demo 内容，不会覆盖已经手调过的英雄和技能资产
- `Fight/Stage 01/Regenerate Demo Content (Overwrite Existing Tuning)`
  强制重建模式，会把当前 demo 英雄、技能、输入和场景重写回样板默认值
- `Fight/Stage 01/Open Battle Scene`
  只打开现有战斗场景，不会重写任何数据

日常调数时，建议优先使用：
- `Open Battle Scene`
- 或 `Generate Demo Content`

除非明确想把 demo 内容重置回模板默认值，否则不要使用：
- `Regenerate Demo Content (Overwrite Existing Tuning)`

## 英雄基础属性在哪里改

每个英雄都有一个 `HeroDefinition` 资源。

示例：
- 火法：`game/Assets/Data/Stage01Demo/Heroes/mage_001_firemage/FIREMAGE.asset`

在 Unity 中选中英雄资源后，重点查看 `Base Stats` 或对应基础属性区域。

常见基础属性包括：
- `Max Health`
- `Attack Power`
- `Defense`
- `Attack Speed`
- `Move Speed`
- `Critical Chance`
- `Critical Damage Multiplier`
- `Attack Range`

这些值决定英雄运行时的基础战斗能力。

说明：
- 运行时实际属性不一定永远等于面板基础值。
- 如果英雄身上存在 Buff / Debuff，运行时会在基础值上继续乘以状态修正。

## 技能数值在哪里改

每个技能都有一个 `SkillData` 资源。

示例：
- 火法小技能：`game/Assets/Data/Stage01Demo/Skills/mage_001_firemage/Ember Burst.asset`
- 火法大招：`game/Assets/Data/Stage01Demo/Skills/mage_001_firemage/Meteor Fall.asset`

在 Unity Inspector 中，技能一般重点看以下几个区域。

### Core

- `Slot Type`
  说明这是普攻技能槽、小技能还是大招
- `Skill Type`
  说明技能属于单体伤害、范围伤害、治疗、Buff 等哪一类
- `Target Type`
  说明技能默认找谁作为目标

这几项主要决定技能行为类型，不直接决定伤害数值。

### Numbers

- `Cast Range`
  技能施法距离
- `Area Radius`
  技能目标区域半径
- `Cooldown Seconds`
  技能冷却时间
- `Min Targets To Cast`
  至少命中多少目标时才允许释放

这组参数主要影响施法机会和作用范围。

### Effects

这是最关键的调数区域。

常见字段：
- `Effect Type`
- `Power Multiplier`
- `Radius Override`
- `Duration Seconds`
- `Tick Interval Seconds`
- `Follow Caster`
- `Status Effects`

其中：

- `Power Multiplier`
  决定这段效果的伤害或治疗倍率，是最核心的数值系数
- `Radius Override`
  如果大于 0，则优先作为这段效果的实际作用半径
- `Duration Seconds`
  持续区域或持续效果会存在多久
- `Tick Interval Seconds`
  持续效果每隔多久结算一次
- `Follow Caster`
  持续区域是否跟着施法者移动

## 火法大招 Meteor Fall 应重点改哪些字段

以 `game/Assets/Data/Stage01Demo/Skills/mage_001_firemage/Meteor Fall.asset` 为例：

### 想调整每一跳伤害

改：
- `Effects -> Element 0 -> Power Multiplier`

### 想调整范围大小

改：
- `Numbers -> Area Radius`
- `Effects -> Element 0 -> Radius Override`

建议：
- 这两个值通常保持一致，避免“AI 认为范围是一个值，持续伤害实际半径又是另一个值”。

### 想调整总持续时间

改：
- `Effects -> Element 0 -> Duration Seconds`

### 想调整每秒跳几次

改：
- `Effects -> Element 0 -> Tick Interval Seconds`

举例：
- `Duration Seconds = 5`
- `Tick Interval Seconds = 1`

通常意味着这个区域大约会结算 5 跳伤害。

### 想调整 AI 什么时候愿意放大

改：
- `Ultimate Decision -> Targeting Type`
- `Ultimate Decision -> Primary Condition`
- `Ultimate Decision -> Secondary Condition`
- `Ultimate Decision -> Fallback`

说明：
- 这组参数影响“什么时候释放”
- 不直接影响“每一跳打多少”

## 当前项目中的伤害算法

当前伤害结算实现位于：
- `game/Assets/Scripts/Core/DamageResolver.cs`

核心公式是：

```text
finalDamage = attackPower * powerMultiplier * critMultiplier * (100 / (100 + defense))
```

其中：

- `attackPower`
  施法者当前攻击力
- `powerMultiplier`
  技能倍率
- `critMultiplier`
  暴击倍率；不暴击时为 `1`
- `defense`
  目标当前防御

### 公式拆解

第一步：

```text
baseDamage = attackPower * powerMultiplier
```

第二步：

```text
damageAfterCrit = baseDamage * critMultiplier
```

第三步：

```text
finalDamage = damageAfterCrit * (100 / (100 + defense))
```

### 防御减伤的含义

当前防御换算为：

```text
defenseMultiplier = 100 / (100 + defense)
```

示例：
- `defense = 0` 时，系数是 `1`
- `defense = 15` 时，系数约为 `0.8696`
- `defense = 30` 时，系数约为 `0.7692`

防御越高，最终伤害越低。

### Power Multiplier 和最终伤害的关系

`Power Multiplier` 与最终伤害是线性关系。

在其他条件不变时：
- 倍率提高 10%，最终伤害也约提高 10%
- 倍率翻倍，最终伤害也约翻倍

示例：

假设：
- 施法者攻击力 = `50`
- 技能倍率 = `0.55`
- 目标防御 = `15`
- 不暴击

则：

```text
baseDamage = 50 * 0.55 = 27.5
finalDamage = 27.5 * (100 / 115) ≈ 23.9
```

如果暴击倍率为 `1.5`：

```text
finalDamage ≈ 27.5 * 1.5 * (100 / 115) ≈ 35.9
```

这会表现为：
- 非暴击约 `23.x`
- 暴击约 `35.x`

## 为什么同一个大招打不同人会有不同数值

同一跳的 `Meteor Fall` 命中不同目标时，最终伤害可能不同，常见原因有：

- 不同目标的 `Defense` 不同
- 是否暴击不同
- 目标身上可能存在运行时状态修正
- 施法者当时的攻击力可能被 Buff / Debuff 影响

因此日志里出现：
- 坦克吃 `18.9`
- 战士吃 `21.3`
- 后排吃 `23.6`
- 某次暴击吃 `35.4`

这是符合当前公式的正常结果。

## 如何用日志核对技能是否正确生效

当前战斗日志会输出：
- 技能施放
- 持续区域创建
- 每次脉冲命中人数
- 每条具体伤害记录

如果要检查火法大招是否正常，建议在导出日志中搜索：

- `Meteor Fall`
- `SkillAreaPulse`

### 正常链路应该长这样

1. 出现一次施法记录
   例如：`cast Meteor Fall`

2. 出现一次区域创建记录
   例如：`created Meteor Fall area skill_area_0000`

3. 每一跳都出现一次脉冲记录
   例如：`Meteor Fall pulse hit 4 target(s)`

4. 每一跳后面都跟着若干条具体伤害记录
   例如：`dealt 23.6 to XXX via Meteor Fall [SkillAreaPulse]`

### 如何判断是否对得上

检查规则：
- `pulse hit N target(s)` 后，应有 `N` 条对应的 `Meteor Fall [SkillAreaPulse]` 伤害记录
- 如果人数减少，通常表示有人死亡或离开范围
- 如果伤害高低不同，先看目标防御和是否暴击，不要直接判断为异常

## 调数建议

第一阶段调数建议按以下顺序来：

1. 先调英雄基础属性
2. 再调技能倍率
3. 再调技能范围、持续时间、跳间隔
4. 最后再调 AI 释放门槛

原因：
- 先把基础战斗强度调顺
- 再把技能体验调顺
- 最后再调“技能是否愿意放”

如果一开始就同时大改属性、倍率、范围和 AI 门槛，很难从日志判断问题出在哪一层。

## 建议的最小验收方法

每次改完火法大招后，至少做一次最小核对：

1. 进入测试战斗
2. 导出日志
3. 搜索 `Meteor Fall`
4. 确认是否出现：
   - 施法记录
   - 区域创建记录
   - 每跳脉冲记录
   - 对应数量的伤害记录
5. 对照当前 `Power Multiplier` 和目标防御，大致判断伤害是否合理

## 关联文件

- `game/Assets/Scripts/Core/DamageResolver.cs`
- `game/Assets/Scripts/Battle/BattleSkillSystem.cs`
- `game/Assets/Scripts/UI/BattleDebugHud.cs`
- `game/Assets/Data/Stage01Demo/Heroes/<heroId>/`
- `game/Assets/Data/Stage01Demo/Skills/<heroId>/`
