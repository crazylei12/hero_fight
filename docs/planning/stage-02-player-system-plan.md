# 第二阶段选手系统基础接入规划

最后更新：2026-04-28

## 文档用途

这份文档记录第二阶段新增目标：`选手系统基础接入`。

它只约束选手系统在当前阶段的最小落地范围，避免提前扩展到培养、成长、转会、合同、赛季经营等完整管理系统。

## 第二阶段目标

第二阶段除真实 BP 界面外，新增一个并行目标：把选手作为 BP 与战斗之间的基础数据层接入。

完成标准：
- 蓝红双方各有 `5` 名选手槽位
- BP 阶段选择的是“选手槽位 + 英雄”的绑定结果，而不是只选择英雄列表
- 选手基础属性可以通过统一计算入口影响进入战斗后的英雄表现
- 选手特性可以通过统一规则影响 BP 展示、BP 判断或战斗表现
- 选手数据不污染 `HeroDefinition`、`SkillData` 或战斗运行时实例的静态配置

## 当前不做

第二阶段选手系统暂时不做：
- 选手培养
- 选手升级、经验或属性成长
- 训练、状态波动、疲劳、伤病
- 转会、合同、薪资、市场
- 赛季、赛程、俱乐部经营
- 复杂队伍阵容管理
- 需要长期存档支撑的选手变化

说明：
- 当前阶段的选手属性是静态配置，用来先打通“选手如何影响 BP 与战斗”的基础通路。
- 未来如果要做培养或属性变化，应先新增对应规划文档，再扩展数据结构和存档。

## 选手基础数据

第一版选手数据应优先保持少而清晰。

建议新增或整理的数据包括：
- 选手 ID
- 选手显示名
- 所属队伍或示例阵容归属
- 头像或占位图
- 攻击属性
- 防御属性
- 当前状态
- 熟练英雄列表
- 选手特性列表
- 简短描述或风格说明

选手静态数据必须独立保存，不和英雄静态配置写在一起。

第一版资源约定：
- 选手 roster 独立保存为 `Resources/Stage01Demo/Stage02AthleteRoster.asset`
- BP 启动时单独读取该 roster
- 示例战斗或直接进入 Battle 时，也应从同一 roster 路径读取默认 10 名选手
- `BattleInputConfig` 只保存或携带本局临时的“选手-英雄绑定结果”，不作为选手基础面板的主数据文件
- `HeroDefinition`、`HeroStatsData`、`SkillData` 不保存选手字段

第一版固定采用以下字段范围：
- `attack`：攻击属性，范围 `0 ~ 50`，默认值 `0`
- `defense`：防御属性，范围 `0 ~ 50`，默认值 `0`
- `condition`：当前状态，范围 `-50 ~ 50`，默认值 `0`
- `heroMasteries`：熟练英雄列表，最多 `4` 条
- `traitIds` 或 `traits`：选手特性引用列表，第一版先预留统一入口

熟练英雄条目至少包含：
- `heroId`
- `mastery`：熟练度，范围 `0 ~ 50`

约束：
- 一个选手最多只能有 `4` 个熟练英雄。
- 熟练度只对当前绑定的同一个 `heroId` 生效。
- 熟练度不是单独的战斗倍率；它先加到选手本局有效攻击和有效防御数值上，再统一换算成战斗加成。
- `attack = 0`、`defense = 0`、`mastery = 0` 都表示没有对应加成，不表示负面惩罚。
- 当前阶段不做通过训练获得新熟练英雄，也不做熟练度成长。
- `attack`、`defense`、`condition` 都是静态配置或示例阵容配置，不做赛季内随机波动。

## 参考公式取舍

根目录 `damage_formula_and_athlete_stats.md` 里的参考游戏有一个核心思路值得保留：
- 选手不是直接改英雄静态资产，而是在战斗开始时给绑定英雄附加一层常驻的赛内修正。
- 攻击、防御、熟练度和状态都通过统一入口换算成战斗属性。
- 熟练度只对当前选手绑定的当前英雄生效。

但本项目第一阶段已经确认：
- 不区分物理伤害和法术伤害。
- 英雄只有统一 `attackPower`，没有独立 `Attack / Magic` 双输出属性。
- `defense` 是统一减伤属性，会直接进入当前 `DamageResolver` 的防御公式。

因此第二阶段选手系统不照搬参考游戏的 `Attack + Magic + MaxHp` 链，而采用更贴合当前工程的统一修正规则：
- 选手攻击主要影响本局 `attackPower`。
- 选手防御只影响本局 `maxHealth`，不影响英雄战斗内的 `defense` 减伤属性。
- 熟练度先叠加到选手本局有效攻击和有效防御数值上，再通过攻击 / 防御公式影响输出和生命。
- 当前状态主要影响战斗节奏，即攻速和移速，不直接增加攻击或防御。
- 特性通过同一套修正通道追加，不允许单个特性绕过统一解析器。

## 战斗修正总结构

战斗开始前，应由统一解析器把每个 `选手 + 英雄` 绑定转换为本局运行时修正。

建议命名：
- `AthleteDefinition`：选手静态数据
- `HeroMasteryEntry`：熟练英雄条目
- `AthleteRosterData`：独立选手 roster 数据文件
- `AthleteTraitDefinition`：选手特性静态数据
- `BattleParticipantBinding`：BP 输出中的单个选手-英雄绑定
- `ResolvedAthleteCombatModifier`：选手对本局运行时英雄的最终修正
- `AthleteCombatModifierResolver`：统一解析器

`ResolvedAthleteCombatModifier` 第一版至少包含：
- `attackPowerModifier`
- `maxHealthModifier`
- `attackSpeedModifier`
- `moveSpeedModifier`
- `bpFitScore`
- `debugBreakdown`

上述 `modifier` 统一使用百分比小数语义：
- `0.10` 表示 `+10%`
- `-0.05` 表示 `-5%`

运行时应用方式：

```text
runtimeAttackPower = heroBaseAttackPower * (1 + finalAttackPowerModifier)
runtimeMaxHealth   = heroBaseMaxHealth   * (1 + finalMaxHealthModifier)
runtimeAttackSpeed = heroBaseAttackSpeed * (1 + finalAttackSpeedModifier)
runtimeMoveSpeed   = heroBaseMoveSpeed   * (1 + finalMoveSpeedModifier)
```

第一版不修改：
- 防御减伤属性 `defense`
- 暴击率
- 暴击伤害
- 攻击距离
- 技能冷却
- 大招策略
- 技能倍率
- 状态效果静态配置

说明：
- 因为当前技能、治疗和普攻都已经围绕 `attackPower` 或统一结算入口工作，所以攻击修正会自然影响普攻、技能伤害和基于攻击力的治疗。
- 选手修正不建议做成普通 `StatusEffect`，因为普通状态会受到死亡清空、持续时间、移除事件等规则影响；选手修正应是本局常驻运行时修正，战斗结束后丢弃。

## 有效攻防数值算法

熟练度先叠加到选手的本局有效攻防数值上。

匹配规则：
- 如果当前绑定英雄的 `heroId` 存在于选手 `heroMasteries` 中，则读取该英雄对应的 `mastery`。
- 如果不存在，则本局 `masteryScore = 0`。
- 每名选手最多配置 `4` 个熟练英雄。

输入范围：

```text
baseAttackScore = clamp(athlete.attack, 0, 50)
baseDefenseScore = clamp(athlete.defense, 0, 50)
masteryScore = matched ? clamp(matchedMastery.mastery, 0, 50) : 0
```

有效攻防：

```text
effectiveAttackScore = clamp(baseAttackScore + masteryScore, 0, 100)
effectiveDefenseScore = clamp(baseDefenseScore + masteryScore, 0, 100)
```

示例：

```text
选手 attack = 30
选手 defense = 40
当前英雄 mastery = 25

effectiveAttackScore = 30 + 25 = 55
effectiveDefenseScore = 40 + 25 = 65
```

说明：
- 熟练度不是额外独立倍率，也不再单独给一份攻击 / 生命修正。
- 不熟练英雄不会受到额外惩罚，只是 `masteryScore = 0`。
- 有效攻防可以高于 `50`，因为熟练度代表该选手在当前英雄上的额外发挥。

## 攻击属性算法

`attack` 表示选手的进攻执行能力，实际计算使用 `effectiveAttackScore`。

基础攻击修正：

```text
attackFromEffectiveAttack = effectiveAttackScore * 0.005
```

结果范围：
- `effectiveAttackScore = 0` 时，`attackFromEffectiveAttack = 0`
- `effectiveAttackScore = 50` 时，`attackFromEffectiveAttack = +0.25`
- `effectiveAttackScore = 100` 时，`attackFromEffectiveAttack = +0.50`

战斗含义：
- 高攻击选手能让该英雄本局普攻、技能伤害和基于攻击力的治疗略微提高。
- 低攻击选手只是加成更少，不会因为攻击属性低而获得负面输出惩罚。
- 攻击属性不直接影响攻速、暴击率或技能释放频率。

示例：

```text
effectiveAttackScore = 55
attackFromEffectiveAttack = 55 * 0.005 = +0.275
=> attackPower +27.5%
```

## 防御属性算法

`defense` 表示选手的抗压、站场和失误控制能力，实际计算使用 `effectiveDefenseScore`。

基础生命修正：

```text
healthFromEffectiveDefense = effectiveDefenseScore * 0.005
```

结果范围：
- `effectiveDefenseScore = 0` 时，`healthFromEffectiveDefense = 0`
- `effectiveDefenseScore = 50` 时，`healthFromEffectiveDefense = +0.25`
- `effectiveDefenseScore = 100` 时，`healthFromEffectiveDefense = +0.50`

战斗含义：
- 防御属性的收益只进入本局最大生命值。
- 防御属性不影响英雄本体 `defense`，避免选手系统改变统一防御减伤公式。
- 防御属性不直接降低敌方暴击率，也不直接影响护盾、治疗或复活时间。

示例：

```text
effectiveDefenseScore = 65
healthFromEffectiveDefense = 65 * 0.005 = +0.325
=> maxHealth +32.5%
```

## 熟练度算法

熟练度表示选手与当前英雄的适配程度。

第一版熟练度不单独输出额外战斗倍率，而是按“有效攻防数值算法”执行：

```text
effectiveAttackScore = clamp(baseAttackScore + masteryScore, 0, 100)
effectiveDefenseScore = clamp(baseDefenseScore + masteryScore, 0, 100)
```

战斗含义：
- 熟练度是 BP 阶段的重要适配依据。
- 熟练度不直接修改英雄技能机制，不解锁额外技能，也不改变英雄职业或标签。
- 熟练度通过提高有效攻击，间接影响普攻、技能伤害和基于攻击力的治疗。
- 熟练度通过提高有效防御，间接提高最大生命值。
- 熟练度不提供额外战斗 `defense` 减伤。

示例：

```text
当前英雄 heroId = marksman_001_longshot
选手 attack = 30
选手 defense = 40
当前英雄 mastery = 25

effectiveAttackScore = 55
effectiveDefenseScore = 65

attackPowerModifier = 55 * 0.005 = +27.5%
maxHealthModifier = 65 * 0.005 = +32.5%
```

## 当前状态算法

`condition` 表示选手本场进入战斗时的临场状态。

输入范围：

```text
conditionScore = clamp(athlete.condition, -50, 50)
```

状态修正：

```text
attackSpeedFromCondition = conditionScore * 0.002
moveSpeedFromCondition   = conditionScore * 0.001
```

结果范围：
- `condition = -50`：攻速 `-10%`，移速 `-5%`
- `condition = 0`：无修正
- `condition = 50`：攻速 `+10%`，移速 `+5%`

战斗含义：
- 状态好时，选手操控的英雄出手更顺、走位更快。
- 状态差时，英雄节奏变慢，更容易错过输出或支援窗口。
- 当前状态不直接增加攻击、防御、生命、暴击或治疗量。
- 当前阶段不做状态随机变化；`condition` 由示例数据或未来外围系统提供。

示例：

```text
condition = 25
attackSpeedFromCondition = 25 * 0.002 = +0.05
moveSpeedFromCondition   = 25 * 0.001 = +0.025
=> attackSpeed +5%，moveSpeed +2.5%
```

## 特性预留算法

选手特性是后续扩展点，第一版可以先只建立数据结构和统一解析入口，不强制做大量具体特性。

特性数据至少需要表达：
- `traitId`
- `displayName`
- `description`
- `triggerScope`：`BPOnly / CombatOnly / Both`
- `triggerCondition`
- `effectType`
- `value`
- `maxStack` 或是否允许叠加
- 调试说明

第一版允许的战斗影响类型只走以下通道：
- `AttackPowerModifier`
- `MaxHealthModifier`
- `AttackSpeedModifier`
- `MoveSpeedModifier`
- `BpFitScoreModifier`

特性触发后，不直接改英雄或技能静态资产，而是追加到 `ResolvedAthleteCombatModifier`：

```text
traitAttackPowerModifier += sum(activeTrait.AttackPowerModifier)
traitMaxHealthModifier   += sum(activeTrait.MaxHealthModifier)
traitAttackSpeedModifier += sum(activeTrait.AttackSpeedModifier)
traitMoveSpeedModifier   += sum(activeTrait.MoveSpeedModifier)
```

预留示例：
- `职业专精`：绑定指定职业英雄时，`attackPowerModifier +0.04`
- `稳健抗压`：`maxHealthModifier +0.05`
- `快速启动`：`attackSpeedModifier +0.04`
- `状态稳定`：当 `condition < 0` 时，负面状态带来的攻速和移速惩罚减半
- `气氛带动`：同队其他选手结算 `condition` 前获得 `+10`，但仍受 `-50 ~ 50` 上限限制

第一版禁止的特性效果：
- 直接修改 `HeroDefinition`
- 直接修改 `SkillData`
- 直接减少复活时间
- 直接修改胜负规则、击杀计分或加时规则
- 绕过统一伤害、治疗、状态或事件系统
- 在战斗中随机成长、永久成长或改变选手静态数据

## 最终修正合成与上限

单个选手-英雄绑定的最终修正按以下顺序计算：

```text
attackPowerModifier =
  attackFromEffectiveAttack
  + traitAttackPowerModifier

maxHealthModifier =
  healthFromEffectiveDefense
  + traitMaxHealthModifier

attackSpeedModifier =
  attackSpeedFromCondition
  + traitAttackSpeedModifier

moveSpeedModifier =
  moveSpeedFromCondition
  + traitMoveSpeedModifier
```

为避免选手系统压过英雄本体差异，第一版必须对最终结果做上限钳制：

```text
attackPowerModifier = clamp(attackPowerModifier, 0, +0.50)
maxHealthModifier   = clamp(maxHealthModifier,   0, +0.50)
attackSpeedModifier = clamp(attackSpeedModifier, -0.15, +0.20)
moveSpeedModifier   = clamp(moveSpeedModifier,   -0.08, +0.08)
```

示例完整计算：

```text
选手 attack = 30
选手 defense = 40
选手 condition = 25
当前英雄熟练度 = 25
无特性触发

effectiveAttackScore = 30 + 25 = 55
effectiveDefenseScore = 40 + 25 = 65

attackFromEffectiveAttack = 55 * 0.005 = +0.275
finalAttackPowerModifier = +0.275

healthFromEffectiveDefense = 65 * 0.005 = +0.325
finalMaxHealthModifier = +0.325

attackSpeedFromCondition = +0.05
finalAttackSpeedModifier = +0.05

moveSpeedFromCondition = +0.025
finalMoveSpeedModifier = +0.025
```

若该英雄基础属性为：

```text
attackPower = 40
maxHealth = 300
defense = 10
attackSpeed = 1.0
moveSpeed = 4.0
```

进入战斗后的选手修正后属性为：

```text
runtimeAttackPower = 40 * 1.275 = 51
runtimeMaxHealth   = 300 * 1.325 = 397.5
runtimeDefense     = 10
runtimeAttackSpeed = 1.0 * 1.05 = 1.05
runtimeMoveSpeed   = 4.0 * 1.025 = 4.10
```

## 对 BP 的影响

选手系统接入 BP 后，BP 的核心结果应从：

`蓝方英雄列表 + 红方英雄列表`

扩展为：

`蓝方 5 个选手-英雄绑定 + 红方 5 个选手-英雄绑定`

BP 阶段至少应体现：
- 左右双方栏展示选手槽位
- 每个选手槽位最终绑定 1 名英雄
- 选中选手或英雄时，能看到基础属性、擅长方向和特性摘要
- 英雄选择时可以显示该英雄与当前选手的适配提示
- 特性可以影响 BP 期的提示、推荐、限制或风险标记

第一版 BP 适配分建议只作为提示，不作为合法性限制。

建议计算：

```text
bpFitScore =
  30
  + effectiveAttackScore * 0.45
  + effectiveDefenseScore * 0.35
  + traitBpFitScoreModifier
```

然后钳制：

```text
bpFitScore = clamp(round(bpFitScore), 0, 100)
```

说明：
- `bpFitScore` 只用于显示“适配好 / 一般 / 风险”。
- `bpFitScore` 不阻止玩家强行选择非熟练英雄。
- 若英雄不在熟练列表中，适配分不会吃到熟练度加成，但也不额外扣分。
- 特性未来可以给适配分加减，但不应直接改写英雄池可选状态。

边界：
- 第一版不做自动 AI 选人教练
- 第一版不要求复杂推荐算法
- 第一版不因为选手特性直接改写英雄池静态数据
- 选手对 BP 的影响应保存在 BP 会话或派生结果中，不写回选手或英雄静态配置

## 对战斗的影响

选手进入战斗前，应通过统一入口把“选手 + 英雄”的绑定转换为战斗输入。

建议规则：
- `BattleInputConfig` 或后续兼容对象需要能表达每个出战槽位对应的选手
- 战斗开始创建运行时英雄时，使用统一解析器计算选手带来的属性或规则修正
- 修正只作用于本局运行时副本，不修改 `HeroDefinition`、`HeroStatsData`、`SkillData`
- 所有选手属性与特性造成的修正应可被日志或调试面板追踪

第一版战斗影响应控制在轻量范围，例如：
- 小幅基础属性修正
- 特定职业或标签下的小幅加成
- 辅助、输出、生存等方向的可读修正

不要在第一版加入难以验证的复杂行为树、隐藏随机成长或长期状态变化。

## 与 BP 界面的关系

`docs/planning/stage-02-bp-interface-plan.md` 仍负责 BP 流程、布局和交互。

本文件负责规定：
- BP 槽位为什么要从“英雄槽”升级为“选手-英雄绑定槽”
- 选手基础属性如何参与 BP 展示和适配判断
- 选手数据如何进入战斗输入
- 选手特性如何通过统一规则影响 BP 或战斗

两个目标应同步推进，但不要互相越界：
- BP UI 不直接裁定战斗数值
- 选手系统不负责 Ban/Pick 顺序
- 最终桥接点仍是统一战斗输入对象

## 建议实现顺序

1. 定义选手静态数据结构和示例选手资产。
2. 让蓝红双方阵容栏显示 5 个选手槽位。
3. 让 BP 结果保存为选手-英雄绑定，而不是单纯英雄列表。
4. 扩展战斗输入对象或兼容桥接对象，携带选手绑定信息。
5. 增加统一选手修正解析器，按本文公式计算攻击、防御、熟练度和状态修正。
6. 让运行时英雄读取 `ResolvedAthleteCombatModifier`，并保证只作用于本局副本。
7. 接入选手特性数据结构与展示；第一版具体特性可以先只做少量样例或暂不启用。
8. 增加调试日志，记录每个选手-英雄绑定最终获得的修正。
9. 再考虑更细的 BP 适配提示和视觉表现。

## 最低验证

每轮涉及选手系统的改动后至少验证：
- 蓝红双方各 5 个选手槽位存在
- 每个槽位可以绑定 1 名英雄
- BP 完成后战斗输入能保留选手-英雄绑定关系
- 选手属性或特性不会修改英雄静态资产
- 战斗中选手修正只作用于本局运行时结果
- 攻击属性只影响本局攻击力通道
- 防御属性只影响本局最大生命值，不影响战斗内 `defense` 减伤属性
- 熟练度只对当前绑定英雄生效，并先叠加到选手本局有效攻击和有效防御数值上
- 不熟练英雄不吃熟练度加成，但也没有额外负面惩罚
- 当前状态只影响攻速和移速，不直接影响攻击、防御或生命
- 最终修正被钳制在本文规定的上限内
- 战斗开始日志或调试输出能追踪选手修正来源
- 未接入培养、成长、经营或存档变化

## 对后续 AI 的要求

- 不要把“选手系统基础接入”扩展成完整经营系统。
- 不要新增训练、成长、合同、转会、薪资等内容。
- 如果需要新增字段，先确认字段服务于 BP 或战斗影响。
- 如果需要新增公式，必须保持小范围、可解释、可日志追踪，并同步更新本文。
- 如果发现选手系统需要改动战斗输入对象，应优先保持与第一阶段战斗核心解耦。
- 不要让选手特性绕过 `AthleteCombatModifierResolver` 或等价统一解析入口。
