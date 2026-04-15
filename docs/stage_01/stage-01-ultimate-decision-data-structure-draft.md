# 第一阶段大招决策数据结构草案

最后更新：2026-04-10

## 文档用途

这份文档用于给出第一阶段“大招目标选择模板 + 释放条件模板 + 兜底规则”的可实现数据结构草案。

它的目标不是直接替代代码实现，而是为后续 AI 或开发者提供一套足够清晰的落地方向，避免实现阶段临时拍脑袋扩字段。

本文件聚焦以下内容：
- `SkillData` 应如何扩展
- 需要哪些决策相关枚举
- 条件模板参数建议如何承载
- 第一阶段实现时哪些字段应该先做，哪些先不要做

## 设计目标

第一阶段这套数据结构应满足：
- 不把大招释放时机写死在职业代码里
- 支持不同英雄复用同一批模板
- 支持少量模板组合，不追求复杂表达式系统
- 参数足够配置化，方便快速调参
- 尽量兼容当前已有 `SkillData` 体系，避免大拆

## 核心思路

建议把大招决策拆成三层：

1. `目标选择`
决定技能对谁或对哪里释放

2. `主释放条件`
决定大招什么时候允许释放

3. `辅助条件 / 兜底规则`
用于做少量补充限制，或避免整局捏大不放

第一阶段不建议做成任意嵌套规则树。

第一阶段建议固定为：
- `1 个目标选择模板`
- `1 个主条件`
- `0 到 1 个辅助条件`
- `0 到 1 个兜底规则`

## SkillData 扩展建议

如果当前 `SkillData` 已经承载基础技能信息，建议不要新建一套完全独立的大招数据对象，而是在 `SkillData` 中增加一个可选的“大招决策配置”字段。

推荐方向：

```csharp
public class SkillData : ScriptableObject
{
    public string SkillId;
    public string SkillName;
    public SkillType SkillType;
    public SkillTargetType TargetType;
    public SkillSlotType SlotType;

    public float CooldownSeconds;
    public float CastRange;
    public float Radius;

    public List<SkillEffectData> Effects;

    public UltimateDecisionConfig UltimateDecision;
}
```

说明：
- `UltimateDecision` 只在 `SkillSlotType.Ultimate` 时使用
- 小技能可以先忽略这个字段
- 这样能尽量减少对现有技能系统的破坏

## UltimateDecisionConfig 草案

建议增加一个统一的大招决策配置对象：

```csharp
[Serializable]
public class UltimateDecisionConfig
{
    public UltimateTargetingTemplate TargetingTemplate;
    public UltimateDecisionConditionData PrimaryCondition;
    public UltimateDecisionConditionData SecondaryCondition;
    public UltimateFallbackRuleData FallbackRule;
    public UltimateConditionCombineMode CombineMode;
}
```

字段建议含义：
- `TargetingTemplate`
  决定目标选择模板
- `PrimaryCondition`
  主释放条件
- `SecondaryCondition`
  可选辅助条件
- `FallbackRule`
  可选兜底规则
- `CombineMode`
  主条件与辅助条件如何组合

第一阶段建议默认：
- `SecondaryCondition` 可为空
- `FallbackRule` 可为空
- `CombineMode` 默认 `PrimaryOnly`

## 目标选择模板枚举

第一阶段建议先做下面这组：

```csharp
public enum UltimateTargetingTemplate
{
    None = 0,
    CurrentTarget = 1,
    LowestHealthEnemyInRange = 2,
    LowestHealthAllyInRange = 3,
    EnemyDensestPosition = 4,
    Self = 5
}
```

### 字段含义建议

- `CurrentTarget`
  使用当前锁定目标
- `LowestHealthEnemyInRange`
  选择施法范围内生命值比例最低的敌人
- `LowestHealthAllyInRange`
  选择施法范围内生命值比例最低的友军
- `EnemyDensestPosition`
  选择敌方最密集区域
- `Self`
  对自己释放

## 释放条件模板枚举

第一阶段建议把你已经确认的 `7 个释放条件模板` 固定为枚举，`fallback` 单独作为独立规则结构：

```csharp
public enum UltimateDecisionConditionType
{
    None = 0,
    EnemyCountInRange = 1,
    AllyCountInRange = 2,
    EnemyLowHealthInRange = 3,
    AllyLowHealthInRange = 4,
    SelfLowHealth = 5,
    TargetIsHighValue = 6,
    InCombatDuration = 7
}
```

说明：
- `FallbackAfterSeconds` 不再建议保留在条件枚举里
- 当前工程实现中，它已经明确拆分到独立的 `UltimateFallbackType / UltimateFallbackData`

## 条件组合枚举

第一阶段不建议做复杂逻辑表达式，建议只支持最小组合模式：

```csharp
public enum UltimateConditionCombineMode
{
    PrimaryOnly = 0,
    AllMustPass = 1,
    AnyPass = 2
}
```

推荐实际使用范围：
- 默认 `PrimaryOnly`
- 少量技能允许 `AllMustPass`
- 第一阶段尽量不要使用 `AnyPass`

## 高价值目标枚举

为了让 `TargetIsHighValue` 不依赖字符串硬编码，建议增加一组简单标签：

```csharp
public enum HighValueTargetType
{
    None = 0,
    Backline = 1,
    Ranged = 2,
    LowDefense = 3,
    LowHealth = 4
}
```

第一阶段说明：
- 不需要把高价值判断做得太复杂
- 只要能覆盖“切后排脆皮”这种典型场景就够了

## 统一条件参数结构

为了避免给每个模板单独做一个类，第一阶段可以先采用“统一参数容器 + 按模板读取”的轻量方案。

推荐草案：

```csharp
[Serializable]
public class UltimateDecisionConditionData
{
    public UltimateDecisionConditionType ConditionType;

    public float SearchRadius;
    public int RequiredUnitCount;
    public float HealthPercentThreshold;
    public float TimeThresholdSeconds;

    public HighValueTargetType HighValueTargetType;
    public bool RequireTargetInCastRange;
}
```

### 统一参数字段说明

- `SearchRadius`
  用于范围检测
- `RequiredUnitCount`
  用于人数阈值
- `HealthPercentThreshold`
  用于低血阈值
- `TimeThresholdSeconds`
  用于交战时长或兜底时长
- `HighValueTargetType`
  用于高价值目标类型
- `RequireTargetInCastRange`
  用于要求当前目标仍可施法命中

### 为什么先用统一参数容器

原因：
- 第一阶段模板数量少
- 快速落地比类型完美更重要
- Inspector 配置会更简单
- 后续如果模板持续增多，再拆成不同参数类也不晚

## 兜底规则结构草案

虽然 `FallbackAfterSeconds` 可以暂时和条件共用一套评估思路，但为了语义更清晰，我更建议单独保留一个兜底结构。

```csharp
[Serializable]
public class UltimateFallbackRuleData
{
    public UltimateFallbackRuleType RuleType;
    public float TriggerAfterSeconds;
    public int OverrideRequiredUnitCount;
    public float OverrideHealthPercentThreshold;
}
```

配套枚举：

```csharp
public enum UltimateFallbackRuleType
{
    None = 0,
    LowerPrimaryThreshold = 1
}
```

说明：
- 第一阶段先只做一种兜底规则就够了
- `LowerPrimaryThreshold` 的含义是：到了某个时间点后，降低主条件门槛

例如：
- 原本 `EnemyCountInRange >= 3`
- 到了第 18 秒后改成 `EnemyCountInRange >= 2`

## 推荐的第一阶段参数读取规则

为避免第一版实现混乱，建议明确约定每种模板读取哪些字段。

### EnemyCountInRange

读取：
- `SearchRadius`
- `RequiredUnitCount`

### AllyCountInRange

读取：
- `SearchRadius`
- `RequiredUnitCount`

### EnemyLowHealthInRange

读取：
- `SearchRadius`
- `HealthPercentThreshold`
- `RequiredUnitCount`

### AllyLowHealthInRange

读取：
- `SearchRadius`
- `HealthPercentThreshold`
- `RequiredUnitCount`

### SelfLowHealth

读取：
- `HealthPercentThreshold`

### TargetIsHighValue

读取：
- `HighValueTargetType`
- `RequireTargetInCastRange`

### InCombatDuration

读取：
- `TimeThresholdSeconds`

### FallbackAfterSeconds

作为独立兜底规则读：
- `TriggerAfterSeconds`
- 兜底覆盖字段

## 示例配置草案

### 法师范围大招

```csharp
UltimateDecisionConfig
{
    TargetingTemplate = UltimateTargetingTemplate.EnemyDensestPosition,
    CombineMode = UltimateConditionCombineMode.PrimaryOnly,
    PrimaryCondition = new UltimateDecisionConditionData
    {
        ConditionType = UltimateDecisionConditionType.EnemyCountInRange,
        SearchRadius = 4.5f,
        RequiredUnitCount = 3
    },
    FallbackRule = new UltimateFallbackRuleData
    {
        RuleType = UltimateFallbackRuleType.LowerPrimaryThreshold,
        TriggerAfterSeconds = 18f,
        OverrideRequiredUnitCount = 2
    }
}
```

### 辅助群疗大招

```csharp
UltimateDecisionConfig
{
    TargetingTemplate = UltimateTargetingTemplate.LowestHealthAllyInRange,
    CombineMode = UltimateConditionCombineMode.AllMustPass,
    PrimaryCondition = new UltimateDecisionConditionData
    {
        ConditionType = UltimateDecisionConditionType.AllyLowHealthInRange,
        SearchRadius = 5f,
        HealthPercentThreshold = 0.4f,
        RequiredUnitCount = 1
    },
    SecondaryCondition = new UltimateDecisionConditionData
    {
        ConditionType = UltimateDecisionConditionType.AllyCountInRange,
        SearchRadius = 5f,
        RequiredUnitCount = 2
    }
}
```

### 刺客收割大招

```csharp
UltimateDecisionConfig
{
    TargetingTemplate = UltimateTargetingTemplate.LowestHealthEnemyInRange,
    CombineMode = UltimateConditionCombineMode.AllMustPass,
    PrimaryCondition = new UltimateDecisionConditionData
    {
        ConditionType = UltimateDecisionConditionType.EnemyLowHealthInRange,
        SearchRadius = 4f,
        HealthPercentThreshold = 0.35f,
        RequiredUnitCount = 1
    },
    SecondaryCondition = new UltimateDecisionConditionData
    {
        ConditionType = UltimateDecisionConditionType.TargetIsHighValue,
        HighValueTargetType = HighValueTargetType.Backline,
        RequireTargetInCastRange = true
    }
}
```

## 第一阶段实现建议

建议优先落地顺序：

1. 先给 `SkillData` 增加 `UltimateDecisionConfig`
2. 先实现 `UltimateTargetingTemplate`
3. 先实现 `PrimaryCondition`
4. 再支持 `FallbackRule`
5. 最后再接 `SecondaryCondition`

这样可以最快拿到第一批可调试结果。

## 第一阶段先不要做的事

- 不要先做嵌套规则树
- 不要先做表达式字符串解析
- 不要先做可视化规则编辑器
- 不要先做超过 1 个辅助条件
- 不要先做大量目标价值评分字段

## 与现有文档的关系

本文件是 `docs/stage_01/stage-01-ultimate-cast-decision-templates.md` 的数据结构补充稿。

两者分工：
- 模板文档负责定义“有哪些模板、它们是什么意思”
- 本文档负责定义“这些模板在数据层怎么表达”

## 贴近当前 Unity 工程的字段命名建议

基于当前 `game/Assets/Scripts/Data/` 下已经存在的实现，当前工程的数据命名风格有这些明显特征：
- 类名采用 `PascalCase`
- 字段名采用 `camelCase`
- `SkillData` 已使用 `skillId`、`displayName`、`description`
- 数值字段已使用 `castRange`、`areaRadius`、`cooldownSeconds`
- 枚举字段已使用 `slotType`、`skillType`、`targetType`
- 列表字段已使用 `effects`

因此大招决策相关字段建议尽量延续这一套风格，不要突然改成另一套命名系统。

### 当前 SkillData 真实字段

当前工程中的 `SkillData` 已存在以下字段：

```csharp
public class SkillData : ScriptableObject
{
    public string skillId;
    public string displayName;
    public string description;

    public SkillSlotType slotType;
    public SkillType skillType;
    public SkillTargetType targetType;

    public float castRange;
    public float areaRadius;
    public float cooldownSeconds;
    public int minTargetsToCast;

    public List<SkillEffectData> effects;
    public bool allowsSelfCast;
}
```

这意味着新方案最好做到：
- 不推翻 `targetType`
- 不推翻 `minTargetsToCast`
- 新字段能和当前 `slotType == Ultimate` 的逻辑自然衔接
- Inspector 里仍然容易读和配置

## 更贴近当前工程的 SkillData 扩展建议

如果要尽量贴近当前工程，我建议把前面那版偏抽象的 `UltimateDecisionConfig` 收敛成更符合现有命名风格的版本：

```csharp
public class SkillData : ScriptableObject
{
    public string skillId = "skill_001_template";
    public string displayName = "Template Skill";
    public string description;

    public SkillSlotType slotType = SkillSlotType.ActiveSkill;
    public SkillType skillType = SkillType.SingleTargetDamage;
    public SkillTargetType targetType = SkillTargetType.NearestEnemy;

    public float castRange = 4f;
    public float areaRadius = 0f;
    public float cooldownSeconds = 6f;
    public int minTargetsToCast = 1;

    public List<SkillEffectData> effects = new();
    public bool allowsSelfCast;

    public UltimateDecisionData ultimateDecision = new();
}
```

说明：
- 字段建议命名为 `ultimateDecision`
- 不建议叫 `ultimateDecisionConfig`，因为当前 `SkillData` 里的字段大多不带 `Config` 后缀
- 这样和 `effects`、`slotType`、`targetType` 的风格更一致

## 更贴近当前工程的决策结构命名建议

### 1. UltimateDecisionData

建议命名：

```csharp
[Serializable]
public class UltimateDecisionData
{
    public UltimateTargetingType targetingType = UltimateTargetingType.UseSkillTargetType;
    public UltimateConditionData primaryCondition = new();
    public UltimateConditionData secondaryCondition = new();
    public UltimateFallbackData fallback = new();
    public UltimateConditionCombineMode combineMode = UltimateConditionCombineMode.PrimaryOnly;
}
```

为什么这样命名：
- 当前工程里状态效果使用的是 `StatusEffectData`
- 所以这里用 `UltimateDecisionData`、`UltimateConditionData`、`UltimateFallbackData` 会更统一
- `targetingType` 也和已有 `skillType`、`targetType` 命名更一致

### 2. 为什么建议加 UseSkillTargetType

当前 `SkillData` 已经有一个通用的 `targetType`。

所以大招目标选择模板不一定每次都要完全覆盖旧字段，建议加一个：

```csharp
public enum UltimateTargetingType
{
    UseSkillTargetType = 0,
    CurrentTarget = 1,
    LowestHealthEnemyInRange = 2,
    LowestHealthAllyInRange = 3,
    EnemyDensestPosition = 4,
    Self = 5
}
```

这样做的好处：
- 如果某个大招仍然适合沿用现有 `targetType`，就不用重复配两套目标规则
- 旧系统到新系统的过渡会更平滑
- 后续 BattleSkillSystem 接入时更容易先兼容旧逻辑

## 更贴近当前工程的条件枚举命名建议

前一版用了 `UltimateDecisionConditionType`，语义没问题，但按当前工程风格可以更短一些。

更推荐：

```csharp
public enum UltimateConditionType
{
    None = 0,
    EnemyCountInRange = 1,
    AllyCountInRange = 2,
    EnemyLowHealthInRange = 3,
    AllyLowHealthInRange = 4,
    SelfLowHealth = 5,
    TargetIsHighValue = 6,
    InCombatDuration = 7
}
```

说明：
- 当前工程里的枚举名都偏短，例如 `SkillType`、`SkillTargetType`、`SkillSlotType`
- 所以这里建议使用 `UltimateConditionType`
- `FallbackAfterSeconds` 更适合移到单独的 fallback 结构里，而不是继续塞在条件枚举里

## 更贴近当前工程的参数结构命名建议

```csharp
[Serializable]
public class UltimateConditionData
{
    public UltimateConditionType conditionType = UltimateConditionType.None;

    [Min(0f)] public float searchRadius = 0f;
    [Min(0)] public int requiredUnitCount = 1;
    [Range(0f, 1f)] public float healthPercentThreshold = 1f;
    [Min(0f)] public float durationSeconds = 0f;

    public HighValueTargetType highValueTargetType = HighValueTargetType.None;
    public bool requireTargetInCastRange = true;
}
```

字段命名理由：
- `searchRadius` 延续 `castRange`、`areaRadius`
- `requiredUnitCount` 比 `minTargetsToCast` 更中性，因为它不只服务于敌方人数判断
- `healthPercentThreshold` 和当前数值命名习惯一致，表达也清楚
- `durationSeconds` 延续 `cooldownSeconds`、`durationSeconds`

## 更贴近当前工程的兜底结构命名建议

```csharp
[Serializable]
public class UltimateFallbackData
{
    public UltimateFallbackType fallbackType = UltimateFallbackType.None;
    [Min(0f)] public float triggerAfterSeconds = 0f;

    public int overrideRequiredUnitCount = 0;
    public float overrideHealthPercentThreshold = -1f;
}
```

配套枚举：

```csharp
public enum UltimateFallbackType
{
    None = 0,
    LowerPrimaryThreshold = 1
}
```

字段命名理由：
- `fallbackType` 和 `slotType`、`skillType` 一致
- `triggerAfterSeconds` 比 `timeThresholdSeconds` 更明确，因为它只表达“多久后触发兜底”
- `overrideRequiredUnitCount` 能直接对应你要的“18 秒后 3 降到 2”

## 与当前 minTargetsToCast 的关系建议

当前 `SkillData` 已经有：

```csharp
public int minTargetsToCast = 1;
```

这个字段现在更像“通用释放底线”。

为了减少旧逻辑破坏，我建议第一阶段这样处理：
- `minTargetsToCast` 继续保留给现有小技能 / 老逻辑使用
- 新的大招模板系统优先读取 `ultimateDecision.primaryCondition.requiredUnitCount`
- 如果某个旧大招还没迁到模板系统，就继续走 `minTargetsToCast`

也就是说：
- 不要马上删 `minTargetsToCast`
- 不要强行让它同时承担所有新模板语义
- 把它当成兼容字段更稳

## 更贴近当前工程的最终推荐草案

如果只给一版最贴近当前项目的字段方案，我建议是下面这套：

```csharp
[Serializable]
public class UltimateDecisionData
{
    public UltimateTargetingType targetingType = UltimateTargetingType.UseSkillTargetType;
    public UltimateConditionData primaryCondition = new();
    public UltimateConditionData secondaryCondition = new();
    public UltimateFallbackData fallback = new();
    public UltimateConditionCombineMode combineMode = UltimateConditionCombineMode.PrimaryOnly;
}

[Serializable]
public class UltimateConditionData
{
    public UltimateConditionType conditionType = UltimateConditionType.None;
    public float searchRadius = 0f;
    public int requiredUnitCount = 1;
    public float healthPercentThreshold = 1f;
    public float durationSeconds = 0f;
    public HighValueTargetType highValueTargetType = HighValueTargetType.None;
    public bool requireTargetInCastRange = true;
}

[Serializable]
public class UltimateFallbackData
{
    public UltimateFallbackType fallbackType = UltimateFallbackType.None;
    public float triggerAfterSeconds = 0f;
    public int overrideRequiredUnitCount = 0;
    public float overrideHealthPercentThreshold = -1f;
}
```

## 建议的实现优先顺序

如果后续开始动代码，建议按当前工程兼容性优先这样落：

1. 先在 `SkillData` 增加 `ultimateDecision`
2. 先增加 `UltimateTargetingType`
3. 先增加 `UltimateConditionType`
4. 先让 `BattleSkillSystem` 在 `slotType == Ultimate` 时优先读取 `ultimateDecision.primaryCondition`
5. 再接入 `fallback`
6. 最后再支持 `secondaryCondition`

这样改动路径最短，也最不容易把当前 demo 先打坏。

## 按当前工程直接实现的执行稿

这一节不是概念建议，而是面向后续编码 AI 的直接实施说明。

目标是：
- 让后续 AI 不需要再自己猜“改哪些文件”
- 尽量按当前代码结构最小代价完成大招模板化
- 保证现有 demo 不会因为一次重构全部失效

当前相关代码入口已经明确在这些文件里：
- `game/Assets/Scripts/Data/SkillData.cs`
- `game/Assets/Scripts/Data/SkillTargetType.cs`
- `game/Assets/Scripts/Battle/BattleSkillSystem.cs`
- `game/Assets/Scripts/Battle/BattleAiDirector.cs`
- `game/Assets/Scripts/Editor/Stage01SampleContentBuilder.cs`

### 实施总原则

- 不要推翻现有 `SkillData` 结构，只做增量扩展
- 不要先重写全部技能逻辑，只先替换 `Ultimate` 的释放判断
- 不要删现有 `targetType` 和 `minTargetsToCast`
- 先让“新大招走模板，旧技能继续可跑”
- 优先保证 `Battle.unity` 和 `BattleBasicAttackOnly.unity` 还能继续用于验证

## 第 1 步：新增数据类型文件

先在 `game/Assets/Scripts/Data/` 新增以下文件：

1. `UltimateDecisionData.cs`
2. `UltimateConditionData.cs`
3. `UltimateFallbackData.cs`
4. `UltimateTargetingType.cs`
5. `UltimateConditionType.cs`
6. `UltimateFallbackType.cs`
7. `UltimateConditionCombineMode.cs`
8. `HighValueTargetType.cs`

要求：
- 命名空间统一使用 `Fight.Data`
- 参数类加 `[System.Serializable]`
- 默认值要让旧技能资产在未配置时也不会直接报错

### 推荐内容

#### UltimateTargetingType.cs

```csharp
namespace Fight.Data
{
    public enum UltimateTargetingType
    {
        UseSkillTargetType = 0,
        CurrentTarget = 1,
        LowestHealthEnemyInRange = 2,
        LowestHealthAllyInRange = 3,
        EnemyDensestPosition = 4,
        Self = 5,
    }
}
```

#### UltimateConditionType.cs

```csharp
namespace Fight.Data
{
    public enum UltimateConditionType
    {
        None = 0,
        EnemyCountInRange = 1,
        AllyCountInRange = 2,
        EnemyLowHealthInRange = 3,
        AllyLowHealthInRange = 4,
        SelfLowHealth = 5,
        TargetIsHighValue = 6,
        InCombatDuration = 7,
    }
}
```

#### UltimateFallbackType.cs

```csharp
namespace Fight.Data
{
    public enum UltimateFallbackType
    {
        None = 0,
        LowerPrimaryThreshold = 1,
    }
}
```

#### UltimateConditionCombineMode.cs

```csharp
namespace Fight.Data
{
    public enum UltimateConditionCombineMode
    {
        PrimaryOnly = 0,
        AllMustPass = 1,
        AnyPass = 2,
    }
}
```

#### HighValueTargetType.cs

```csharp
namespace Fight.Data
{
    public enum HighValueTargetType
    {
        None = 0,
        Backline = 1,
        Ranged = 2,
        LowDefense = 3,
        LowHealth = 4,
    }
}
```

#### UltimateConditionData.cs

```csharp
using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class UltimateConditionData
    {
        public UltimateConditionType conditionType = UltimateConditionType.None;

        [Min(0f)] public float searchRadius = 0f;
        [Min(0)] public int requiredUnitCount = 1;
        [Range(0f, 1f)] public float healthPercentThreshold = 1f;
        [Min(0f)] public float durationSeconds = 0f;

        public HighValueTargetType highValueTargetType = HighValueTargetType.None;
        public bool requireTargetInCastRange = true;
    }
}
```

#### UltimateFallbackData.cs

```csharp
using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class UltimateFallbackData
    {
        public UltimateFallbackType fallbackType = UltimateFallbackType.None;
        [Min(0f)] public float triggerAfterSeconds = 0f;

        public int overrideRequiredUnitCount = 0;
        public float overrideHealthPercentThreshold = -1f;
    }
}
```

#### UltimateDecisionData.cs

```csharp
using System;

namespace Fight.Data
{
    [Serializable]
    public class UltimateDecisionData
    {
        public UltimateTargetingType targetingType = UltimateTargetingType.UseSkillTargetType;
        public UltimateConditionData primaryCondition = new UltimateConditionData();
        public UltimateConditionData secondaryCondition = new UltimateConditionData();
        public UltimateFallbackData fallback = new UltimateFallbackData();
        public UltimateConditionCombineMode combineMode = UltimateConditionCombineMode.PrimaryOnly;
    }
}
```

## 第 2 步：扩展 SkillData

修改：
- `game/Assets/Scripts/Data/SkillData.cs`

新增字段：

```csharp
[Header("Ultimate Decision")]
public UltimateDecisionData ultimateDecision = new UltimateDecisionData();
```

放置建议：
- 放在 `Effects` 段之后也可以
- 或者放在 `Numbers` 与 `Effects` 之间
- 重点是不要打乱现有核心字段的含义

要求：
- 不删除 `targetType`
- 不删除 `minTargetsToCast`
- 不删除 `allowsSelfCast`

原因：
- 这三个字段当前已经被 `BattleSkillSystem` 和示例内容生成器使用
- 先保留兼容层，后续再逐步迁移

## 第 3 步：给 RuntimeHero 增加交战计时

修改：
- `game/Assets/Scripts/Heroes/RuntimeHero.cs`

新增一个运行时字段，用于支持 `InCombatDuration`：

```csharp
public float CombatEngagedSeconds { get; private set; }
```

实现建议：
- 英雄活着且存在有效敌方目标时，持续累加
- 死亡时清零
- `ResetToSpawn()` 时清零
- 当前目标为空时可按需要缓慢回零，或直接清零

第一阶段最简单可行方案：
- 只要 `CurrentTarget != null && !CurrentTarget.IsDead`，就在 `Tick()` 里累加
- 否则归零

这样够支撑大招模板，不需要先做完整“交战状态机”

## 第 4 步：在 BattleSkillSystem 内拆出大招模板判断入口

修改：
- `game/Assets/Scripts/Battle/BattleSkillSystem.cs`

### 当前问题

当前逻辑里，大招与小技能的核心区别主要是：
- `TryCastSkill()` 里先尝试大招
- 大招使用 `requireHighValueCast: true`
- `HasHighValueOpportunity()` 用 `skillType + affectedTargets.Count` 做简单判断

这还不是模板系统。

### 要改成的结构

建议把 `TryCastSpecificSkill()` 拆成两条路径：

1. 普通技能路径
- 继续沿用现有逻辑

2. 大招路径
- 优先读取 `skill.ultimateDecision`
- 使用模板化条件判断是否释放

### 推荐重构方向

把这里：

```csharp
if (TryCastSpecificSkill(context, caster, caster.Definition?.ultimateSkill, battleManager, requireHighValueCast: true))
```

改成更清晰的两段：

```csharp
if (TryCastUltimate(context, caster, caster.Definition?.ultimateSkill, battleManager))
{
    return true;
}

return TryCastActiveSkill(context, caster, caster.Definition?.activeSkill, battleManager);
```

然后新增：
- `TryCastUltimate(...)`
- `TryCastActiveSkill(...)`

## 第 5 步：在 BattleSkillSystem 中实现大招目标模板选择

新增方法建议：

```csharp
private static RuntimeHero SelectUltimatePrimaryTarget(BattleContext context, RuntimeHero caster, SkillData skill)
```

优先读取：
- `skill.ultimateDecision.targetingType`

行为建议：

### UseSkillTargetType

直接复用现有：

```csharp
return SelectPrimaryTarget(context, caster, skill);
```

### CurrentTarget

```csharp
return caster.CurrentTarget != null && !caster.CurrentTarget.IsDead
    ? caster.CurrentTarget
    : SelectPrimaryTarget(context, caster, skill);
```

### LowestHealthEnemyInRange

复用现有 `FindLowestHealth(... includeAllies: false ...)`

### LowestHealthAllyInRange

复用现有 `FindLowestHealth(... includeAllies: true ...)`

### EnemyDensestPosition

先复用现有 `FindDensestEnemyAnchor(...)`

### Self

直接返回 `caster`

要求：
- 第一阶段不要把目标模板实现分散到职业 AI 里
- 目标模板必须由 `BattleSkillSystem` 或其直接调用的统一方法处理

## 第 6 步：在 BattleSkillSystem 中实现条件模板评估

新增统一入口：

```csharp
private static bool EvaluateUltimateCondition(
    BattleContext context,
    RuntimeHero caster,
    SkillData skill,
    RuntimeHero primaryTarget,
    UltimateConditionData condition,
    bool useFallbackOverride)
```

### 每个模板的建议判断方式

#### EnemyCountInRange

统计：
- 以 `primaryTarget` 为中心
- 或在 `primaryTarget == null` 时以 `caster` 为中心
- 半径使用 `condition.searchRadius > 0 ? condition.searchRadius : skill.areaRadius`

判断：
- 范围内敌人数 >= `requiredUnitCount`

#### AllyCountInRange

与上面同理，但统计友军

#### EnemyLowHealthInRange

统计满足：
- 范围内敌方
- 血量比例 <= `healthPercentThreshold`

再判断数量是否 >= `requiredUnitCount`

#### AllyLowHealthInRange

同理，统计友方

#### SelfLowHealth

判断：
- `caster.CurrentHealth / caster.MaxHealth <= healthPercentThreshold`

#### TargetIsHighValue

优先检查 `primaryTarget`

判断建议：
- `Backline`
  `Mage / Support / Marksman`
- `Ranged`
  `Mage / Marksman / Support`
- `LowDefense`
  防御值低于一个简单阈值，或先用职业近似代替
- `LowHealth`
  当前血量比例 <= 0.5

如果 `requireTargetInCastRange == true`
- 还要额外检查和施法者距离 <= `skill.castRange`

#### InCombatDuration

判断：
- `caster.CombatEngagedSeconds >= condition.durationSeconds`

## 第 7 步：在 BattleSkillSystem 中实现辅助条件与组合逻辑

新增方法建议：

```csharp
private static bool EvaluateUltimateDecision(
    BattleContext context,
    RuntimeHero caster,
    SkillData skill,
    RuntimeHero primaryTarget)
```

逻辑：

1. 先评估主条件
2. 如果有 fallback，且到时机了，则允许主条件临时降低门槛
3. 再根据 `combineMode` 决定是否评估辅助条件

建议规则：

### PrimaryOnly

- 只看主条件

### AllMustPass

- 主条件与辅助条件都要通过

### AnyPass

- 第一阶段可以先实现
- 但默认不要给示例英雄用

## 第 8 步：实现兜底规则

当前只实现一种：
- `LowerPrimaryThreshold`

实现方式：
- 不要直接改写配置对象
- 在评估主条件时，根据 `fallback.triggerAfterSeconds` 决定是否使用“临时覆盖后的阈值”

建议做法：

新增一个内部辅助方法：

```csharp
private static int GetEffectiveRequiredUnitCount(
    RuntimeHero caster,
    SkillData skill,
    UltimateConditionData condition,
    UltimateFallbackData fallback)
```

判断：
- 如果没有 fallback，返回原值
- 如果还没到触发秒数，返回原值
- 到了触发秒数，且 `overrideRequiredUnitCount > 0`，返回覆盖值

同理如果未来要支持低血阈值放宽，也可以加：
- `GetEffectiveHealthPercentThreshold(...)`

规则补充：
- 第一阶段所有大招默认都应配置 `fallback`
- 也就是说，`UltimateFallbackData` 在数据结构层面虽然允许保留 `None`
- 但在项目实际配置规范里，应把“每个大招都写 fallback”视为强制要求

## 第 9 步：改造现有大招释放入口

在 `TryCastUltimate(...)` 里，推荐顺序如下：

1. 判空
2. 判冷却
3. 选择主目标
4. 如果主目标为空且不允许无目标释放，则返回 false
5. 收集作用目标
6. 评估 `ultimateDecision`
7. 条件通过才执行 `ExecuteSkill`

重要：
- 大招不再走 `requireHighValueCast`
- `HasHighValueOpportunity()` 可以保留给旧兼容路径，但新大招模板应不再依赖它

## 第 10 步：保留旧技能逻辑兼容

为了避免一次改动太大，推荐兼容规则：

### 对 ActiveSkill

继续走当前逻辑：
- `SelectPrimaryTarget`
- `CollectTargets`
- `minTargetsToCast`

### 对 Ultimate

按以下优先级：

1. 如果 `ultimateDecision.primaryCondition.conditionType != None`
   走新模板系统
2. 否则走旧的 `HasHighValueOpportunity()` 逻辑

这样做的好处：
- 你可以逐个迁移示例大招
- 不需要一口气把所有旧资产全改完

## 第 11 步：改造 Stage01SampleContentBuilder

修改：
- `game/Assets/Scripts/Editor/Stage01SampleContentBuilder.cs`

这是第一阶段里非常关键的一步，因为后续验证严重依赖自动生成的示例内容。

### 要做的改动

在创建大招资产后，补充给 `skill.ultimateDecision` 赋值。

建议至少先迁以下几个大招：

#### 法师大招

- `targetingType = EnemyDensestPosition`
- `primaryCondition = EnemyCountInRange`
- `requiredUnitCount = 3`
- `searchRadius = areaRadius`
- `fallback = LowerPrimaryThreshold`
- `triggerAfterSeconds = 18f`
- `overrideRequiredUnitCount = 2`

#### 坦克大招

- `targetingType = EnemyDensestPosition`
- `primaryCondition = EnemyCountInRange`
- `requiredUnitCount = 2`
- `secondaryCondition = InCombatDuration`
- `durationSeconds = 2f`
- `combineMode = AllMustPass`

#### 刺客大招

- `targetingType = LowestHealthEnemyInRange`
- `primaryCondition = EnemyLowHealthInRange`
- `healthPercentThreshold = 0.35f`
- `requiredUnitCount = 1`
- `secondaryCondition = TargetIsHighValue`
- `highValueTargetType = Backline`
- `combineMode = AllMustPass`

#### 辅助大招

- `targetingType = LowestHealthAllyInRange`
- `primaryCondition = AllyLowHealthInRange`
- `healthPercentThreshold = 0.4f`
- `requiredUnitCount = 1`
- `secondaryCondition = AllyCountInRange`
- `requiredUnitCount = 2`
- `combineMode = AllMustPass`

#### 射手大招

- `targetingType = EnemyDensestPosition`
- `primaryCondition = EnemyCountInRange`
- `requiredUnitCount = 3`
- `fallback = LowerPrimaryThreshold`

#### 战士大招

- `targetingType = EnemyDensestPosition`
- `primaryCondition = EnemyCountInRange`
- `requiredUnitCount = 2`
- `fallback = LowerPrimaryThreshold`

### 可以暂缓迁移

- 暂不需要给每个英雄都做复杂辅助条件
- 但不应让任何模板英雄的大招缺少 fallback

## 第 12 步：决定是否扩 BattleAiDirector

第一阶段建议：
- 尽量少改 `BattleAiDirector`
- 它继续负责职业风格、索敌和站位
- 不要把大招模板判断塞进去

只有一种小范围扩展可以接受：
- 如果你想复用现有“找敌方最密集点”的逻辑，可以在 `BattleAiDirector` 里把 `FindClusteredEnemy` 提升成公共可调用方法

但即便这样：
- 最终调用权仍应留在 `BattleSkillSystem`

## 第 13 步：建议保留或新增的日志

为了便于验证，建议在大招模板路径增加少量调试日志或事件说明。

至少建议记录：
- 当前大招是否走模板路径
- 主条件是否通过
- 辅助条件是否通过
- 是否触发 fallback

第一阶段最轻量方案：
- 先用 `Debug.Log` 或现有调试日志入口
- 不强制新增新的 battle event 类型

## 第 14 步：最低验证顺序

编码完成后，最少按下面顺序验证：

1. 项目仍可编译
2. `Fight -> Stage 01 -> Generate Demo Content` 仍能成功执行
3. `Battle.unity` 仍能正常开战
4. 法师大招会优先等敌方聚集再释放
5. 法师大招在拖到约 18 秒后会更容易释放
6. 辅助大招不再在队友健康时过早释放
7. 刺客大招更偏向收割低血后排
8. 老的小技能逻辑没有被破坏
9. `BattleBasicAttackOnly.unity` 仍不受影响

## 第 15 步：完成标准

满足下面这些条件，才算这一轮大招模板化实现完成：

- `SkillData` 已支持 `ultimateDecision`
- 新枚举和参数类已进入 `Fight.Data`
- `BattleSkillSystem` 已支持模板化大招判断
- 至少 4 个示例大招已迁移到模板系统
- 现有战斗 demo 仍可运行
- 没有把大招判断重新写回职业硬编码
- 文档与代码字段命名保持一致

## 对后续实现 AI 的最终执行指令

如果你现在开始编码，按下面顺序做，不要自己改顺序：

1. 新增大招决策相关数据类型
2. 扩展 `SkillData`
3. 给 `RuntimeHero` 增加 `CombatEngagedSeconds`
4. 重构 `BattleSkillSystem`，拆出 `TryCastUltimate`
5. 实现目标模板选择
6. 实现主条件评估
7. 实现 fallback
8. 实现辅助条件组合
9. 修改 `Stage01SampleContentBuilder` 迁移示例大招
10. 跑 demo 验证并补必要日志

不要先做：
- 自定义可视化编辑器
- 复杂规则树
- 大规模重写 AI 系统
- 删除旧字段兼容层

## 给后续实现 AI 的一句话指令

如果开始实现这套系统，优先做“统一配置对象 + 少量稳定枚举 + 统一参数容器”，先把第一批 6 个模板英雄跑通，再考虑更复杂的决策表达能力。

## 当前已实现契约补充

基于当前工程已经落地的版本，再额外补充以下实现契约：

- `SkillData` 的技能效果现已统一收束为 `effects`
- 每个技能通过一组 `SkillEffectData` 描述实际效果，而不是继续在 `SkillData` 顶层零散扩字段
- 当前已落地的标准效果模板为：
  - `DirectDamage`
  - `DirectHeal`
  - `ApplyStatusEffects`
  - `RepositionNearPrimaryTarget`
  - `PersistentAreaDamage`
- 这意味着像“持续 5 秒、每 1 秒脉冲一次、范围跟随施法者”的火雨效果，也应优先作为标准效果模板表达，而不是再给单个英雄补专用字段
- 后续如果再出现新的技能效果，默认规则应为：
  - 先判断能否用现有标准效果模板组合表达
  - 若不能表达，先补新的通用 `SkillEffectType` / `SkillEffectData` 参数，再让具体英雄技能去引用
  - 不应直接把新效果先写成英雄专属分支或顶层临时字段

- `UltimateFallbackData` 现已支持第二段兜底字段：
  - `secondaryTriggerAfterSeconds`
  - `secondaryOverrideRequiredUnitCount`
  - `secondaryOverrideHealthPercentThreshold`

- 这意味着当前工程中的大招兜底已经不是只能单段触发
- 现在可以表达：
  - `30 秒后人数阈值从 3 降到 2`
  - `45 秒后人数阈值再从 2 降到 1`

- 当前法师示例大招已经按这套方式落地：
  - 目标选择：`Self`
  - 主条件：`EnemyCountInRange >= 3`
  - 效果模板：`PersistentAreaDamage`
  - 持续时间：`5 秒`
  - Tick 间隔：`1 秒`
  - 第一段兜底：`30 秒降到 2`
  - 第二段兜底：`45 秒降到 1`

- 当前阶段的统一规范应视为：
  - 大招释放时机优先模板化
  - 技能实际效果优先模板化
  - 英雄只负责“引用哪种模板与参数”，不负责拥有专属效果实现

- 当前主动释放型大招的运行时约定为：
  - 一局一次
  - `cooldownSeconds` 字段继续保留
  - 该字段当前主要承担数据结构一致性和未来被动大招冷却承载的预留作用
