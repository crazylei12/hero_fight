# 0416 检查记录

最后更新：2026-04-16

## 检查范围

这份检查记录用于给后续 AI 或审阅者快速确认：

- `docs/today_work/0416.md` 里描述的圣职者相关改动是否真的成立
- 圣职者英雄实现、技能实现、护盾与持续区域等新增状态/机制是否和文档一致
- 这轮改动里最值得优先修的逻辑问题是什么

本次检查重点参考：

- `docs/today_work/0416.md`
- `docs/planning/stage-01-combat-rules-decisions.md`
- `docs/planning/stage-01-hero-spec-decisions.md`
- `docs/planning/stage-01-status-effect-decisions.md`
- `docs/heroes/support/sunpriest.md`
- `game/Assets/Scripts/Data/BasicAttackData.cs`
- `game/Assets/Scripts/Data/SkillType.cs`
- `game/Assets/Scripts/Data/SkillEffectType.cs`
- `game/Assets/Scripts/Data/SkillEffectData.cs`
- `game/Assets/Scripts/Data/StatusEffectType.cs`
- `game/Assets/Scripts/Heroes/RuntimeStatusEffect.cs`
- `game/Assets/Scripts/Heroes/RuntimeHero.cs`
- `game/Assets/Scripts/Battle/RuntimeBasicAttackProjectile.cs`
- `game/Assets/Scripts/Battle/BattleAiDirector.cs`
- `game/Assets/Scripts/Battle/BattleSimulationSystem.cs`
- `game/Assets/Scripts/Battle/BattleBasicAttackSystem.cs`
- `game/Assets/Scripts/Battle/BattleSkillSystem.cs`
- `game/Assets/Scripts/UI/BattleDebugHud.cs`
- `game/Assets/Scripts/Editor/Stage01SampleContentBuilder.cs`
- `game/Assets/Data/Stage01Demo/Heroes/support_001_sunpriest/Sunpriest.asset`
- `game/Assets/Data/Stage01Demo/Skills/support_001_sunpriest/Radiant Heal.asset`
- `game/Assets/Data/Stage01Demo/Skills/support_001_sunpriest/Sun Blessing.asset`

## 当前检查结论

结论可以先看一句话：

- **这轮实现的大方向是对的：圣职者、友方治疗普攻通用化、护盾入统一状态系统、持续区域收束到统一抽象，这几件事基本都落到了代码和资产里。**

更具体的判断是：

- **圣职者当前已经不是占位设计，而是“治疗普攻 + 单体治疗护盾 + 持续范围治疗大招”的真实可运行模板。**
- **友方普攻确实走了统一基础攻击系统，不是只给圣职者硬写的一套隐藏技能。**
- **护盾也确实进入了统一状态系统，不是技能脚本里偷偷维护的局部变量。**
- **持续区域抽象也基本收束成了“创建区域 + 区域脉冲载荷”的方向。**
- **但当前仍有 3 个值得优先修的逻辑问题，会直接影响行为语义和后续调试。**

## 对“圣职者已经按 0416 目标落地”的检查结论

结论：

- **这条成立。**

已确认做对的部分：

- `Sunpriest.asset` 当前已经是：
  - `support_001_sunpriest`
  - `Support`
  - 远程投射物普攻
  - 普攻效果类型为 `Heal`
  - 普攻目标类型为 `LowestHealthAlly`
- `Radiant Heal.asset` 当前已经是：
  - 单体直接治疗
  - 外加 `Shield`
- `Sun Blessing.asset` 当前已经是：
  - `AreaHeal`
  - 通过 `CreatePersistentArea`
  - 脉冲类型为 `DirectHeal`
  - 目标阵营为 `Allies`
- `docs/heroes/support/sunpriest.md` 与当前资产参数基本一致

这说明：

- 当前圣职者的设计语义已经落进实际英雄资产和技能资产
- 重新生成 demo content 时，也不会自动回退到旧版辅助占位配置

## 对“友方普攻已经做成通用能力”的检查结论

结论：

- **这条成立。**

已确认做对的部分：

- `BasicAttackData` 允许配置：
  - `effectType`
  - `targetType`
- `BattleSimulationSystem` 会按普攻目标类型重选目标
- `BattleBasicAttackSystem` 会按普攻效果类型走伤害或治疗
- 投射物普攻仍走统一 `RuntimeBasicAttackProjectile` 流程

这说明：

- 这次不是给 Sunpriest 单独塞了一套技能式治疗普攻
- 结构上已经允许后续其他英雄复用“友方普攻治疗”能力

## 对“护盾已经进入统一状态系统”的检查结论

结论：

- **这条成立。**

已确认做对的部分：

- `StatusEffectType` 已正式包含 `Shield`
- `Radiant Heal` 通过 `ApplyStatusEffects` 给目标附加护盾
- `RuntimeHero.ApplyDamage(...)` 会先消耗护盾，再扣生命值
- 护盾值存放在 `RuntimeStatusEffect` 里，而不是放回 `HeroDefinition` 或技能局部变量

这说明：

- 这轮护盾接入方式符合状态文档希望的“统一状态系统表达”
- 没有走“技能自己另存一份 shieldValue”的坏方向

## 对“持续区域抽象已经收束”的检查结论

结论：

- **这条目前基本成立。**

已确认做对的部分：

- `SkillEffectType` 当前使用：
  - `CreatePersistentArea`
- `SkillEffectData` 当前已经能描述：
  - 区域目标阵营
  - 区域脉冲效果类型
  - 持续时间
  - tick 间隔
  - 附带状态载荷
- `BattleSkillSystem.ResolveSkillAreaPulse(...)` 当前已经支持：
  - 直接伤害
  - 直接治疗
  - 仅施加状态
  - 直接伤害/治疗后再附带状态

这说明：

- 当前结构已经明显好于为每一种区域效果继续追加 `PersistentAreaHeal` / `PersistentAreaBuff` 之类的新枚举
- 从代码结构看，后续范围 Buff / Debuff 也有继续承接的空间

## 当前最值得优先修的 3 个问题

### 1. 圣职者在全队满血时仍会持续出手“无效治疗普攻”

结论：

- **这是当前最值得优先修的行为问题。**

原因：

- `BattleSimulationSystem` 对友方治疗普攻会始终给出一个友军目标
- 当没有受伤友军时，会退回到最近健康友军作为站位锚点
- 但后续逻辑没有区分“只拿来站位”和“真的应该发起普攻”
- 所以 Sunpriest 进范围后仍会正常走普攻与投射物流，只是最后 `ApplyHealing` 返回 0

影响：

- 行为语义和 `sunpriest.md` 中“当健康友军只作为站位锚点”不完全一致
- 会制造额外的空打、额外投射物和额外日志噪声

### 2. `Untargetable` 目前挡不住自疗 / 自护盾

结论：

- **这是当前最明确的规则边界风险。**

原因：

- 友方普攻和友方技能在“目标是自己”时，都放宽了一层 `CanBeDirectTargeted` 检查
- 结果是如果后续英雄或技能真正开始使用 `Untargetable`
- 那么单位仍然可能对自己打治疗普攻、给自己上单体治疗/护盾

影响：

- 这和状态文档里“不可选中会阻断成为普攻和单体直接技能合法目标”的语义存在边界冲突
- 当前代码实际上默认了一个“自施法例外”，但这个例外没有被文档明确承认

### 3. 护盾清空事件与持续区域脉冲时机都不够干净

结论：

- **这是当前最需要尽早收口的时序问题。**

护盾侧的问题：

- 护盾在 `ApplyDamage` 中被正确消耗
- 但护盾被打空后，只是把持续时间置零
- 真正从状态列表移除、并抛出 `StatusRemovedEvent`，要等下一次 `Tick`

持续区域侧的问题：

- `RuntimeSkillArea` 初始 `TimeUntilNextTickSeconds = 0`
- 这会让区域在创建后几乎立刻脉冲一次
- 并且在持续时间归零的那一帧，仍有可能再多脉冲一次

影响：

- 会让护盾移除日志晚一拍
- 会让 `Sun Blessing` 的实际跳数和玩家直觉上的“每 1 秒跳一次、持续 5 秒”不完全一致
- 同类问题也会波及周期状态 `RuntimeStatusEffect`

## 其他检查判断

### 1. 生成器与现有资产是否一致

结论：

- **这条成立。**

已确认做对的部分：

- `Stage01SampleContentBuilder.cs` 中：
  - `ConfigureSupportBasicAttack(...)`
  - `CreateSupportActiveSkill(...)`
  - `CreateSupportUltimateSkill(...)`
  - `ConfigureSupportUltimate(...)`
  都已和当前 `Sunpriest.asset`、`Radiant Heal.asset`、`Sun Blessing.asset` 对齐

这说明：

- 后续 regenerate 不会把圣职者打回旧版占位配置
- 当前资产和生成器不是双轨漂移状态

### 2. 旧普攻与旧区域伤害技能是否有明显回归风险

结论：

- **目前没有看到“已经明显被改坏”的直接证据，但存在时序类回归风险。**

说明：

- 普通打敌人的普攻主流程仍保持原来的统一路径
- 火法区域技能也仍走同一套 `RuntimeSkillArea`
- 当前更像是新增的治疗区域、护盾、友方普攻把一些时序和合法性边界放大了
- 也就是说，风险点主要集中在：
  - tick 时机
  - 自身目标合法性例外
  - 满血时的友方普攻空打

## 补充检查：远程受击后撤 / 拉扯逻辑

对应提交：

- `35466df`
- commit message: `Add threat-based ranged retreat AI`
- `1f71d58`
- commit message: `Refine ranged retreat trigger rules`

### 一句话结论

- **这版“远程受击后撤”已经做成统一 AI 逻辑了，不是单英雄特例；也继续守住了“只影响投射物普攻英雄”这条硬边界。**
- **而且这轮补修确实把前一版最值得担心的两个问题收住了：DOT 不再刷新 retreat threat，远程对远程正常互点也不会轻易触发双后撤。**
- **这次复检里，我没有再看到同级别的明确代码阻断问题；剩余主要是调参与实机观感风险。**

### 已确认成立的部分

- `RuntimeHero` 已增加短时 threat 记忆：
  - `LastThreatSource`
  - `LastThreatTimeSeconds`
  - `ActiveRetreatThreatSource`
  - `RecordThreat(...)`
  - `TryGetRecentThreat(...)`
- threat 记录入口已经接进：
  - 普攻有效伤害
  - 技能直接伤害
- `BattleAiDirector` 已加入统一 retreat 判定与 retreat 方向计算
- `BattleSimulationSystem` 也确实把 retreat 分支插在：
  - 技能尝试之后
  - 默认前压 / 普攻之前

这说明：

- 这次不是给某个英雄硬写一段特殊后撤脚本
- 当前实现方向和 `0416.md` 后半段草案与补充修正规则已经基本一致

### 这轮补修后已确认收口的 2 个点

#### 1. DOT / 持续伤害不再刷新 retreat threat

结论：

- **这条现在已经成立。**

说明：

- `BattleSimulationSystem.ResolveDamageOverTime(...)` 当前只处理：
  - DOT 伤害结算
  - DOT 伤害日志
  - DOT 击杀归属
- 这里已经没有再调用 `RecordIncomingThreat(...)`
- 现在会刷新 threat 的入口只剩：
  - `BattleBasicAttackSystem` 中的普攻有效伤害
  - `BattleSkillSystem` 中的技能直接伤害

这说明：

- 前一版“DOT 会把远程持续推向异常保守”的主要问题，这轮已经实际修掉了

#### 2. 远程对远程正常互点不会轻易触发 retreat

结论：

- **这条现在也基本成立。**

说明：

- `BattleAiDirector.ShouldRetreatFromRecentThreat(...)` 现在不会只因为“刚刚被打到”就直接触发后撤
- 新增的 `IsThreatEligibleForRetreat(...)` 明确把 threat 分成两类：
  - 非投射物 threat：允许按原逻辑判断后撤
  - 投射物 threat：只有在压进异常近距离时才算值得后撤
- 当前这个“异常近距离”又通过：
  - `RangedThreatUnsafeDistanceFactor`
  - `RangedThreatUnsafeDistanceMinimum`
  做了统一阈值收口

这说明：

- 前一版最容易出现的“两个远程互点一下就一起后撤，再一起走回去”的机械行为，这轮已经有了明确的结构性修正

### 当前剩余更值得继续盯的点

#### 1. Support / 治疗型投射物英雄仍然会进入这套 retreat 规则

结论：

- **这现在更像调参与职责语义风险，不再是明确 bug。**

说明：

- 当前 retreat 的硬边界仍然是 `basicAttack.usesProjectile`
- 这意味着 `Sunpriest` 这类治疗型投射物英雄仍然在规则覆盖范围内
- 但和前一版不同的是，现在：
  - DOT 不再持续刷新 threat
  - 远程 threat 也不是默认都能触发 retreat

影响判断：

- 风险还在，但已经从“代码上明显过度保守”下降成“需要实机观察 support 观感是否合适”

#### 2. “空闲状态”的近似定义仍然偏粗

结论：

- **这仍然是当前最主要的行为调优点。**

说明：

- 当前近似定义基本仍然等于：
  - 本帧没有先放技能
  - `AttackCooldownRemainingSeconds > 0.15f`
  - 允许移动
- 这套定义仍然不是正式动作状态机

影响判断：

- 某些本该原地等下一发出手的远程，理论上仍可能因为这个近似定义而过早后撤
- 但这更像后续实机调参与动作层细化的问题，不是本轮发现的明确逻辑错误

### 其他判断

#### 1. threat 记录时机总体是干净的

结论：

- **这条这次可以从“基本成立”升级为“成立”。**

说明：

- 当前只在“有效伤害真正生效后”记录 threat
- 治疗、护盾、友方来源不会误记成 threat
- DOT / 持续伤害也已经从 retreat threat 刷新入口里移出
- 这条语义和 `0416.md` 里的预期是一致的

#### 2. retreat 本身暂时仍不像会制造额外 TargetChanged 噪声

结论：

- **目前没看到它会直接制造大量额外 `TargetChangedEvent`。**

说明：

- retreat 分支本身只是短时移动，没有强制在 retreat 时额外重选目标
- 当前更大的问题仍然是“会不会退得太多”，而不是“会不会每帧疯狂换目标”

### 本轮关于后撤逻辑的最终结论

- **这轮实现已经把 threat retreat 做成了统一 AI 增量，不是英雄特例，这一点是成立的。**
- **而且 `1f71d58` 这轮补修，已经真正把“DOT threat 持续刷新”和“远程互点双后撤”这两个上一版的关键问题收住了。**
- **当前没有再看到同级别的明确代码阻断问题；剩余更像 support 观感和空闲判定粗粒度这类调参与实机观察问题。**
- **在没有 Unity 实机观察前，它仍更适合作为 `0416` 当日方案记录保留，而不是立刻升级成正式战斗规则。**

## 建议后续 AI 的复核顺序

如果后续 AI 需要快速继续这轮工作，可按以下顺序：

1. 先读 `docs/today_work/0416.md`
2. 再读 `docs/heroes/support/sunpriest.md`
3. 再核对 `Sunpriest.asset`、`Radiant Heal.asset`、`Sun Blessing.asset`
4. 然后重点看：
   - `BattleSimulationSystem.cs`
   - `BattleBasicAttackSystem.cs`
   - `BattleSkillSystem.cs`
   - `RuntimeHero.cs`
   - `RuntimeStatusEffect.cs`
   - `RuntimeSkillArea.cs`
   - `BattleAiDirector.cs`
5. 最后优先复核：
   - 满血友军锚点是否还会触发真实普攻
   - `Untargetable` 是否应阻断自疗 / 自护盾
   - 护盾移除事件与区域 tick 是否需要收口
   - support / 治疗型投射物英雄的 retreat 观感是否仍偏保守
   - “空闲状态”近似定义是否会让远程单位过早后撤

一句话总结：

- **0416 这轮关于 ranged retreat 的主方向没有跑偏，上一版最明显的两处逻辑风险已经被收住；现在更需要继续观察的，是观感和调参，而不是再先定性为代码有明显错误。**

## 补充检查：击飞 / 定向位移实现

对应交接说明：

- `docs/today_work/0416.md`
- 其中“补充交接：击飞 / 定向位移实现”一节

本次额外核对的主要文件：

- `game/Assets/Scripts/Data/SkillType.cs`
- `game/Assets/Scripts/Data/SkillEffectType.cs`
- `game/Assets/Scripts/Data/SkillEffectData.cs`
- `game/Assets/Scripts/Data/StatusEffectType.cs`
- `game/Assets/Scripts/Heroes/RuntimeForcedMovement.cs`
- `game/Assets/Scripts/Heroes/RuntimeHero.cs`
- `game/Assets/Scripts/Battle/BattleSkillSystem.cs`
- `game/Assets/Scripts/Battle/BattleSimulationSystem.cs`
- `game/Assets/Scripts/Battle/BattleEvents.cs`
- `game/Assets/Scripts/UI/BattleView.cs`
- `game/Assets/Scripts/UI/BattleDebugHud.cs`
- `game/Assets/Scripts/Editor/Stage01SampleContentBuilder.cs`
- `game/Assets/Data/Stage01Demo/Skills/tank_001_ironwall/Shield Bash.asset`
- `game/Assets/Data/Stage01Demo/Skills/tank_001_ironwall/Ground Lock.asset`

### 一句话结论

- **这轮“击飞 / 定向位移”实现的大方向是对的：`KnockUp` 已经和 `Stun` 的旧写法分离，当前语义基本符合“硬控 + 可选独立位移”的正式文档约束。**
- **但我看到了 1 个明确问题和 1 个结构性风险，前者值得优先修，后者值得在后续继续收口。**

## 对“击飞是否真的和眩晕分开”的检查结论

结论：

- **这条基本成立。**

已确认做对的部分：

- `StatusEffectType` 已正式加入 `KnockUp`
- `StatusEffectCatalog` 里，`KnockUp` 和 `Stun` 一样接入了统一硬控 flag：
  - `BlocksMovement`
  - `BlocksBasicAttacks`
  - `BlocksSkillCasts`
- `SkillEffectType` 已新增：
  - `ApplyForcedMovement`
- `BattleSkillSystem.ExecuteSkillEffect(...)` 已把：
  - 控制语义
  - 强制位移
  拆成两个独立执行入口
- 坦克样例技能资产当前也确实是：
  - `DirectDamage`
  - `ApplyForcedMovement`
  - `ApplyStatusEffects(KnockUp)`
  这三个效果串起来

这说明：

- 当前实现不是再把“击飞”偷偷当成老 `Stun` 技能名的换皮
- 现在的实现语义已经接近文档要求的：
  - `KnockUp` 负责硬控
  - 推开 / 拉近走独立位移效果

## 对“运行时边界是否合理”的检查结论

结论：

- **这条大体成立。**

已确认做对的部分：

- `RuntimeHero` 已新增：
  - `IsUnderForcedMovement`
  - `VisualHeightOffset`
  - 强制位移 tick
- `BattleSimulationSystem` 里，单位在强制位移期间会直接跳过本帧后续决策
- `RuntimeHero.ResetToSpawn()`、`MarkDead()` 都会清掉：
  - `activeForcedMovement`
  - `VisualHeightOffset`
- 强制位移落点最终仍会经过 `Stage01ArenaSpec.ClampPosition(...)`

这说明：

- 当前边界基本符合 `0416.md` 里说的“强制位移期间不继续执行自发移动 / 普攻 / 技能决策”
- 死亡、复活、回出生点这几个关键清理口目前看是接上的

## 对“表现层是否保持最小且安全”的检查结论

结论：

- **这条成立。**

已确认做对的部分：

- `VisualHeightOffset` 当前只存在于 `RuntimeHero` 的表现辅助字段
- `BattleView.SyncHero(...)` 只把它用于：
  - `VisualRoot.localPosition`
  - `FootUiRoot.localPosition`
- 战斗中的索敌、受击、范围判定、位移落点仍然只看地面位置 `CurrentPosition`

这说明：

- 当前“飞到空中”的主要含义仍然只是视觉抬升
- 这轮没有偷偷把它扩成未被文档约束的 3D 物理命中系统

## 对“数据契约和旧资产兼容性”的检查结论

结论：

- **大体成立。**

已确认做对的部分：

- `SkillType` 和 `SkillEffectType` 都是显式扩字段，不是覆盖旧字段
- `SkillEffectData` 新增的强制位移字段都有安全默认值：
  - `forcedMovementDirection = AwayFromSource`
  - 数值字段默认 `0`
- 当前两个坦克样例技能资产读取路径是通的，YAML 里也已经直接写入：
  - `skillType: 7`
  - `effectType: 5`
  - `effectType: 11`（`KnockUp`）

这说明：

- 旧技能资产不会因为新增字段而立刻被破坏
- 当前样例资产和运行时读取路径没有明显断链

## 当前最值得优先指出的 2 个点

### 1. `Stage01SampleContentBuilder` 对 `KnockUp` 的“首次创建路径”有断链风险

结论：

- **这是这轮实现里最明确、最值得优先修的问题。**

原因：

- `CreateSkill(...)` 会先调用 `AddDefaultEffectsForSkill(skill, powerMultiplier)`
- 但 `AddDefaultEffectsForSkill(...)` 的 `switch` 目前只覆盖：
  - `SingleTargetDamage`
  - `AreaDamage`
  - `SingleTargetHeal`
  - `AreaHeal`
  - `Dash`
- 它没有覆盖 `SkillType.KnockUp`
- 而 `CreateKnockUpSkill(...)` 在 `overwriteExistingContent == false` 时会直接返回，不会再补上：
  - 伤害
  - 强制位移
  - `KnockUp` 状态

影响：

- 现有仓库里的两个坦克技能资产现在是对的，因为它们已经被覆盖生成过
- 但如果在新工程或首次生成 demo content 的路径里只走“不覆盖已有内容”的创建逻辑，`KnockUp` 技能有机会被生成为空效果技能

判断：

- **这不是运行时击飞逻辑本身的问题，而是样例内容生成器对新技能类型的接入没补完整。**

### 2. 强制位移期间的行为限制还没有完全收束到统一查询入口

结论：

- **这更像结构性风险，不是当前已触发的明显 bug。**

原因：

- `RuntimeHero.CanMove` 当前已经把 `IsUnderForcedMovement` 算进去了
- 但 `CanAttack` / `CanCastSkills` 本身没有直接包含 `IsUnderForcedMovement`
- 现在之所以行为正确，主要依赖 `BattleSimulationSystem.TickHero(...)` 里的：
  - `if (hero.IsUnderForcedMovement) return;`

影响：

- 当前主循环里没看到明显漏判
- 但如果后续别的模块只看：
  - `CanAttack`
  - `CanUseActiveSkill`
  - `CanUseUltimate`
  而没先经过 `BattleSimulationSystem` 顶层短路，就可能出现强制位移期间的门禁语义不完全一致

判断：

- **当前能工作，但和状态文档希望的“统一行为判定入口”相比，还差半步。**

## 对样例技能参数的判断

### 1. `Shield Bash`

结论：

- **参数看起来是合理的。**

说明：

- 伤害倍率 `1.0`
- 击飞时长 `0.9s`
- 位移距离 `1.1`
- 位移时长 `0.2s`
- 抬升高度 `0.45`

判断：

- 这组参数更像一个近战撞击型小技能
- 有明确“顶一下 + 稍微打退 + 有一点抬升”的感觉
- 目前没有看到明显离谱到会破坏第一阶段节奏

### 2. `Ground Lock`

结论：

- **当前更需要实机观察，但参数本身还没明显越界。**

说明：

- 伤害倍率 `1.6`
- 击飞时长 `1.4s`
- 位移距离 `0.4`
- 位移时长 `0.24s`
- 抬升高度 `0.72`
- 范围半径 `2.8`

判断：

- 它明显比小技能更偏“范围打断”
- 但因为水平位移只有 `0.4`，当前更主要的强度来源还是群体硬控而不是大范围推飞
- 这更像平衡风险，不像实现错误

## 这轮关于击飞实现的最终结论

- **最值得保留的点是：当前实现已经把 `KnockUp` 收成统一硬控语义，把水平位移收成独立 `ApplyForcedMovement`，并且表现层高度偏移也保持在安全边界内。**
- **最可能有风险的点是：`Stage01SampleContentBuilder` 对 `KnockUp` 的首次创建路径没有补完整，导致新建样例技能时存在“空技能”风险。**
- **从当前代码看，我没有发现这轮击飞实现与正式文档存在明显正面冲突。**
- **如果要继续扩“带方向击飞”，下一步最小增量应该继续落在 `SkillEffectData.ForcedMovementDirectionMode` 与 `BattleSkillSystem.GetForcedMovementDirection(...)` 这一层，而不是把方向语义重新塞回 `KnockUp` 状态本体。**

## 补充检查：骑士英雄落地

对应交接说明：

- `docs/today_work/0416.md`
- 其中“补充交接：骑士英雄落地”一节

本次额外核对的主要文件：

- `docs/heroes/tank/ironwall.md`
- `game/Assets/Scripts/Data/SkillTargetType.cs`
- `game/Assets/Scripts/Battle/BattleSkillSystem.cs`
- `game/Assets/Scripts/Heroes/RuntimeHero.cs`
- `game/Assets/Scripts/Editor/Stage01SampleContentBuilder.cs`
- `game/Assets/Data/Stage01Demo/Heroes/tank_001_ironwall/Ironwall.asset`
- `game/Assets/Data/Stage01Demo/Skills/tank_001_ironwall/Shield Bash.asset`
- `game/Assets/Data/Stage01Demo/Skills/tank_001_ironwall/Ground Lock.asset`

### 一句话结论

- **这轮骑士改动的主语义已经基本落对：`Shield Bash` 已切到单体伤害 + 眩晕，`Iron Oath` 已切到全体友军防御 Buff，主动大招“一局一次”也已经在运行时、生成器和样例资产里统一收口。**
- **但我看到了 1 个明确高风险问题：样例内容生成器现在并不能真正稳定复用旧的 `Ground Lock.asset` 路径，这和交接文档里“有意保留旧路径”的说法不一致。**
- **另外还有 1 个前向语义风险：`AllAllies / AllEnemies` 机制已经通用了，但对未来“全体直接技能 + 不可选中”的边界还没有完全收口。**

## 对“全体友军 / 全体敌军是否真的成为通用机制”的检查结论

结论：

- **这条成立。**

已确认做对的部分：

- `SkillTargetType` 已新增：
  - `AllEnemies`
  - `AllAllies`
- `BattleSkillSystem.SelectPrimaryTarget(...)` 已补这两个目标类型入口
- `BattleSkillSystem.CollectTargets(...)` 已为全体目标单独走：
  - `IsGlobalTeamTargeting(...)`
  - `MatchesGlobalTeamTarget(...)`
- `BattleSkillSystem.IsPrimaryTargetStillValid(...)`
  - 对 `AllAllies / AllEnemies` 已有单独分支
- 当前实现没有看到“只给骑士写死”的单英雄特判

这说明：

- 这轮不是只为 `Iron Oath` 糊一个专用分支
- 结构上已经允许后续别的英雄复用“全体友军 / 全体敌军”目标类型

## 对“骑士小技能是否符合文档”的检查结论

结论：

- **这条成立。**

已确认做对的部分：

- `Shield Bash.asset` 当前已经是：
  - `skillType: Stun`
  - `targetType: NearestEnemy`
  - `castRange: 1.8`
  - `cooldownSeconds: 8`
- 技能效果当前是：
  - `DirectDamage`
  - `powerMultiplier = 0.8`
  - `ApplyStatusEffects`
  - `StatusEffectType.Stun`
  - `durationSeconds = 1`
- 当前资产里已经不再包含：
  - `ApplyForcedMovement`
  - `KnockUp`
  - 范围击飞旧配置

这说明：

- 当前小技能已经从旧的击飞样例正确切回“单点打断型眩晕”
- 当前 YAML 与 `docs/heroes/tank/ironwall.md` 是一致的

## 对“骑士大招是否符合文档”的检查结论

结论：

- **这条成立。**

已确认做对的部分：

- 当前大招资产内部已经是：
  - `m_Name: Iron Oath`
  - `skillId: skill_tank_ultimate_ironoath`
  - `displayName: Iron Oath`
- `targetType` 当前是：
  - `AllAllies`
- `skillType` 当前是：
  - `Buff`
- 状态效果当前是：
  - `StatusEffectType.DefenseModifier`
  - `magnitude = 40`
  - `durationSeconds = 8`
- 当前没有再残留：
  - 范围击飞
  - `KnockUp`
  - 强制位移
  - 护盾 / 无敌 / 治疗

这说明：

- 当前 `Iron Oath` 已经不是旧版 `Ground Lock` 的范围控制大招
- 它现在的运行时语义和英雄文档已经对齐到“全队防御 Buff”

## 对“主动释放型大招是否已经统一为一局一次”的检查结论

结论：

- **这条成立。**

已确认做对的部分：

- `RuntimeHero.CanUseUltimate()` 当前只看：
  - `Definition.ultimateSkill != null`
  - `!HasCastUltimate`
  - `CanCastSkills`
- `RuntimeHero.StartSkillCooldown(...)` 对大招分支会直接：
  - `HasCastUltimate = true`
- 当前没有再看到“大招冷却结束后可再次释放”的旧判断残留
- `Stage01SampleContentBuilder.CreateSkill(...)` 当前会把：
  - `slotType == Ultimate`
  - 统一生成 `cooldownSeconds = 0`
- 当前各样例主动大招资产也都已经是：
  - `cooldownSeconds: 0`

这说明：

- 现在需要看的确实已经不是“骑士大招 CD 是否合理”
- 而是“主动大招是否一局一次”，而这一点当前已经收住

## 对“骑士大招释放判断是否收口到最新要求”的检查结论

结论：

- **这条成立。**

已确认做对的部分：

- `ConfigureTankUltimate(...)` 当前把大招决策写成：
  - `targetingType = EnemyDensestPosition`
  - `primaryCondition = AllyCountInRange`
  - `searchRadius = 6`
  - `requiredUnitCount = 3`
- fallback 当前是：
  - `triggerAfterSeconds = 45`
  - `overrideRequiredUnitCount = 2`
- 没有再残留：
  - 血量阈值判断
  - 额外的 secondary health 条件

这说明：

- 当前决策逻辑已经按交接要求简化成“以敌方密集点为锚，统计附近友军数量”
- `45 秒后 3 人降到 2 人` 这条 fallback 也已经实际接上

## 对“敌方锚点 + 全队 Buff”的组合是否自洽的检查结论

结论：

- **这条成立，而且是这轮最值得保留的结构点。**

已确认做对的部分：

- 决策阶段用 `EnemyDensestPosition` 只负责找判断锚点
- `CountUnitsInRange(...)` 的中心点取的是该锚点位置
- 执行阶段 `CollectTargets(...)` 一旦进入 `AllAllies` 分支，就会直接收集全部存活友军
- 当前没有看到“只给锚点附近友军上 Buff”的混写问题

这说明：

- 当前大招不是“范围 Buff 伪装成全队 Buff”
- 这套“决策锚点”和“实际施加目标”分离的写法是正确的，也值得保留

## 当前最值得优先指出的 2 个点

### 1. `Stage01SampleContentBuilder` 现在并不能真正稳定保留旧的 `Ground Lock.asset` 路径

结论：

- **这是这轮骑士实现里最明确、最值得优先修的问题。**

原因：

- `Stage01SampleContentBuilder.GetSkillAssetPath(...)` 现在仍按：
  - `displayName`
  计算技能资产路径
- 对骑士大招来说，这意味着生成器会去找：
  - `Iron Oath.asset`
- 但当前仓库里真实存在、并仍被英雄资产引用的文件是：
  - `Ground Lock.asset`
- 所以一旦重新生成 demo content：
  - 非覆盖模式下会额外新建一份 `Iron Oath.asset` 孤儿资产
  - 覆盖模式下还可能把 `Ironwall.asset` 的引用改切到新路径

影响：

- 这和 `0416.md` 里“有意保留旧路径”的说法不一致
- 也会让后续工具链、人工排查和资产追踪变得混乱

判断：

- **这不是数值或平衡问题，而是明确的资产路径与生成器契约问题，优先级高于继续调技能参数。**

### 2. `AllAllies / AllEnemies` 对未来“不可选中 + 全体直接技能”的边界还没有完全收口

结论：

- **这更像前向语义风险，不是当前骑士已经触发的 bug。**

原因：

- 当前 `CollectTargets(...)` 对 `AllAllies / AllEnemies` 会直接收集全队存活单位
- `IsPrimaryTargetStillValid(...)` 对这两类目标也直接放行
- 这让 `Iron Oath` 现在能正确给全体友军上 Buff
- 但如果以后有“全体敌军直接技能”出现：
  - 它可能不会复检 `CanBeDirectTargeted`
  - 从而和“不可选中应阻断直接目标技能”的正式文档边界产生冲突

影响：

- 当前骑士大招是安全的，因为它本来就是全队 Buff
- 但这套通用目标机制如果继续扩到别的英雄，后面还需要补一层“全体技能是否属于直接目标语义”的收口

判断：

- **当前实现已经够用，但还不能说这套全体目标语义在所有未来技能上都彻底收稳。**

## 这轮关于骑士实现的最终结论

- **最值得保留的点是：`AllAllies / AllEnemies` 已经不是骑士特例，骑士大招的“敌方密集点作判断锚点 + 实际给全体友军上 Buff”这套结构也是自洽的。**
- **最值得优先修的点是：`Stage01SampleContentBuilder` 和旧的 `Ground Lock.asset` 路径现在并不真正兼容，重新生成内容时存在新建重复资产或改写引用的风险。**
- **从当前最终仓库状态看，骑士技能参数、运行时语义和“一局一次大招”规则本身都已经与正式文档基本对齐。**
- **如果下一步只做一个最小增量，优先应该落在样例内容生成器的技能资产路径兼容逻辑，而不是先继续调骑士参数或再扩新的骑士特例。**

## 补充检查：状态模块收口

对应交接说明：

- `docs/today_work/0416.md`
- 其中“补充交接：状态模块收口”一节

本次额外核对的主要文件：

- `game/Assets/Scripts/Heroes/StatusEffectSystem.cs`
- `game/Assets/Scripts/Heroes/RuntimeHero.cs`
- `game/Assets/Scripts/Heroes/RuntimeStatusEffect.cs`
- `game/Assets/Scripts/Battle/BattleSimulationSystem.cs`
- `game/Assets/Scripts/Battle/BattleSkillSystem.cs`
- `game/Assets/Scripts/Battle/BattleBasicAttackSystem.cs`
- `docs/planning/stage-01-status-effect-decisions.md`

### 一句话结论

- **这轮重构不是假收口：`RuntimeHero` 的状态查询、状态应用、状态 tick、护盾吸收与过期清理，当前确实已经主要走 `StatusEffectSystem` 统一入口。**
- **但这次收口还没有把所有状态语义彻底收干净，我看到了 3 个值得直接记下的问题：周期状态重施加会重置 tick 进度、死亡清状态不会逐个发移除事件、以及目标已被打到 0 血时仍可能先发“状态已施加”事件。**
- **因此当前更准确的判断是：结构已经比重构前清楚很多，但状态事件流和 DOT / HOT 的细粒度语义还没有完全收稳。**

## 对“这次收口是否真的减少了职责发散”的检查结论

结论：

- **这条成立。**

已确认做对的部分：

- `RuntimeHero` 当前的以下入口都已改成委托给 `StatusEffectSystem`：
  - `HasHardControl`
  - `HasStatus(...)`
  - `ApplyStatusEffect(...)`
  - 属性修正读取
  - 行为门禁查询
  - 状态 tick
  - 护盾吸收
  - 过期状态移除
- `StatusEffectSystem` 当前集中承接了：
  - `HasBehaviorFlag(...)`
  - `GetMultiplier(...)`
  - `TryApplyStatus(...)`
  - `Tick(...)`
  - `ConsumeShield(...)`
  - `RemoveExpiredStatuses(...)`
  - `ClearStatuses(...)`
- 当前没有再看到旧版那种“`RuntimeHero` 和新系统同时各留一套同名状态私有逻辑”的双轨并存

这说明：

- 这轮重构确实把最容易继续发散的状态职责先收进了一个明确模块
- `RuntimeHero` 现在更接近“运行时容器 + 对外状态接口”而不是继续堆细节实现

## 对“当前模块边界是否合理”的检查结论

结论：

- **大体合理，但还不是最终收口形态。**

已确认做对的部分：

- `StatusEffectType / StatusEffectData`
  继续只负责静态类型与配置数据
- `RuntimeStatusEffect`
  继续只代表单个运行时状态实例
- `StatusEffectSystem`
  开始承担统一运行时入口
- `RuntimeHero`
  继续保留状态列表本身和运行时宿主职责

当前仍保留的边界现状：

- 周期状态的实际效果结算仍主要在：
  - `BattleSimulationSystem.ResolvePeriodicStatusTick(...)`
- 状态添加/移除事件发布仍主要在：
  - `BattleSkillSystem`
  - `BattleSimulationSystem`
  - `BattleBasicAttackSystem`

这说明：

- 当前分层已经比之前清楚
- 但 `StatusEffectSystem` 还没有发展成“状态系统唯一事实层 + 唯一事件出口”的最终形态

## 对“`RuntimeHero` 和 `StatusEffectSystem` 的耦合方式是否可接受”的检查结论

结论：

- **当前可接受，但属于偏务实型收口，不算特别优雅。**

原因：

- `RuntimeHero` 通过：
  - `internal MutableStatusEffects`
  暴露可写状态列表给 `StatusEffectSystem`
- 从封装角度看，这确实带来了一层显式耦合
- 但从当前阶段目标看，它换来了：
  - 不重写整套状态实例生命周期
  - 不额外引入新的容器类
  - 低风险把逻辑先集中到一个入口

判断：

- **这更像“第一阶段能接受的工程性折中”，不是当前最优先要否掉的问题。**
- **真正更值得优先修的，仍然是状态语义和事件流的一致性。**

## 当前最值得优先指出的 3 个点

### 1. 周期状态重施加会重置 tick 进度，可能吞掉一跳

结论：

- **这是当前最明确的运行时语义问题之一。**

原因：

- `StatusEffectSystem.TryApplyStatus(...)` 在重复施加、且达到 `maxStacks` 时，会调用：
  - `RuntimeStatusEffect.Refresh(...)`
- `RuntimeStatusEffect.Refresh(...)` 当前除了刷新持续时间和数值外，还会把：
  - `TimeUntilNextTickSeconds`
  直接重置成完整 `TickIntervalSeconds`

影响：

- 如果某个 DOT / HOT 本来再过很短时间就该跳一次
- 在重施加后，这一跳可能被整体延后
- 从效果上看，会发生“只刷新持续时间”的同时，顺手把 tick 进度也重置了

判断：

- **这和状态文档里“重施加主要刷新持续时间”的语义不完全一致。**
- **它不会让系统彻底坏掉，但会悄悄改变 DOT / HOT 的强度与节奏。**

### 2. 死亡时清空状态不会逐个发布 `StatusRemovedEvent`

结论：

- **这是当前最值得优先修的事件流问题。**

原因：

- `RuntimeHero.MarkDead(...)` 当前直接调用：
  - `StatusEffectSystem.ClearStatuses(this)`
- 而 `ClearStatuses(...)` 当前只是直接清空状态列表
- 没有像 `RemoveExpiredStatuses(...)` 或 `StatusEffectSystem.Tick(...)` 那样逐个触发过期回调

影响：

- 单位死亡瞬间身上的：
  - `Shield`
  - `Invulnerable`
  - `Untargetable`
  - `Stun`
  - `DOT / HOT`
  等状态会被静默移除
- `BattleDebugHud` 和其他事件订阅者看不到这些状态的结束事件
- 对状态生命周期排查会产生断口

判断：

- **这比 static 设计或列表暴露方式更值得优先修。**
- **因为它直接影响所有状态结束事件的一致性。**

### 3. 目标已被打到 0 血时，仍可能先发“状态已施加”事件，再立刻死亡清空

结论：

- **这是当前日志与状态语义不够干净的问题。**

原因：

- `BattleSkillSystem.ApplyDamageToTargets(...)` 当前顺序是：
  - 先伤害结算
  - 再 `ApplyStatuses(...)`
  - 最后若生命值已到 0 再 `MarkDead(...)`
- DOT 击杀路径在 `BattleSimulationSystem.ResolveDamageOverTime(...)` 也有类似模式

影响：

- 会出现：
  - `StatusAppliedEvent` 已经发出
  - 但目标随后立刻死亡
  - 状态又被死亡清理静默移除
- 从日志看，就会像“状态成功上到了一个马上死掉且没有移除事件的单位”上

判断：

- **这更像调试完整性问题，而不是大范围逻辑错误。**
- **但它和上一条叠加后，会让状态日志明显变脏。**

## 对“这轮是否无意改变了现有状态语义”的检查结论

结论：

- **大部分状态的主语义没有被明显改坏，但周期状态重施加的细节确实发生了可见变化。**

已确认未明显变坏的部分：

- `Stun / KnockUp`
  仍继续通过统一行为门禁阻断移动、普攻和施法
- `Invulnerable`
  仍主要通过 `CanReceiveDamage` / `PreventsDamage` 进入统一受伤入口
- `Untargetable`
  仍主要通过 `CanBeDirectTargeted` 影响索敌与直接目标合法性
- `Shield`
  仍继续在统一受伤入口里先吸收伤害

当前发生细节变化的部分：

- `DOT / HOT`
  在“重施加 + 刷新持续时间”时，现在会一并重置 tick 计时

这说明：

- 这轮确实主要是结构整理
- 但不是 100% 纯搬运；周期状态的节奏细节已经受到了影响

## 这轮关于状态模块收口的最终结论

- **最值得保留的点是：`StatusEffectSystem` 现在已经成为真实的统一运行时入口，`RuntimeHero` 也开始回到“运行时宿主”而不是“状态规则堆放处”的位置。**
- **最值得优先修的点是：死亡清状态不发逐个 `StatusRemovedEvent`，这个问题会直接破坏状态生命周期日志的一致性。**
- **第二优先级的问题是：周期状态重施加会重置 tick 进度，导致 DOT / HOT 的实际节奏和“只刷新持续时间”的预期不完全一致。**
- **如果下一步只做一个最小增量，我会优先收口状态移除事件流，而不是先去动 `StatusEffectSystem` 的 static 形态。**
