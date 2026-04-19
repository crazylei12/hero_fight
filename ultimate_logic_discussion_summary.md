# hero_fight 大招释放逻辑讨论整理

本文档整理了本次关于 `hero_fight` 项目中“英雄大招释放逻辑”的讨论内容，供另一个 AI 继续修改代码或设计方案时参考。

---

## 1. 当前项目里大招释放逻辑的实际位置

最初以为仓库主要是文档规划，后来确认：

- 真正的运行时代码在：
  - `game/Assets/Scripts/Battle/BattleManager.cs`
  - `game/Assets/Scripts/Battle/BattleSimulationSystem.cs`
  - `game/Assets/Scripts/Battle/BattleSkillSystem.cs`
  - `game/Assets/Scripts/Heroes/RuntimeHero.cs`
  - `game/Assets/Scripts/Data/SkillData.cs`
  - `game/Assets/Scripts/Data/UltimateDecisionData.cs`
  - `game/Assets/Scripts/Data/UltimateConditionData.cs`
  - `game/Assets/Scripts/Data/UltimateFallbackData.cs`

### 当前调用链

`BattleManager.Update()`
→ `BattleSimulationSystem.Tick()`
→ `TickHero()`
→ `BattleSkillSystem.TryCastSkill()`
→ 优先 `TryCastUltimate()`
→ 如果大招没释放，再尝试小技能

也就是说，大招判定的真正入口是：

- `BattleSkillSystem.TryCastUltimate()`

---

## 2. 当前大招系统已经实现了什么

当前代码中，大招并不是“冷却好了立刻放”，而是已经做成了一套模板化 + 概率化机制。

### 2.1 统一判定节奏

`BattleSkillSystem.cs` 中当前有以下常量：

- `UltimateInitialLockoutSeconds = 6f`
- `UltimateDecisionIntervalSeconds = 0.75f`
- `UltimateDecisionJitterSeconds = 0.5f`
- `UltimateBaseReleaseChance = 0.3f`
- `UltimateExtraUnitReleaseChance = 0.15f`
- `UltimateFirstFallbackBonus = 0.2f`
- `UltimateSecondFallbackBonus = 0.5f`
- `UltimateSecondaryPriorityBonus = 0.25f`

含义：

- 开局前 6 秒不尝试开大
- 之后不是每帧判定，而是每隔约 0.75 秒判定一次
- 每个英雄的判定时刻会加一个最多 0.5 秒的随机抖动

### 2.2 大招不是必放，而是概率释放

当前大招流程大致是：

1. 到达当前英雄的下一次大招判定时机
2. 检查该英雄的大招条件是否满足
3. 如果满足，不直接释放，而是调用 `RollUltimateCastChance()`
4. `RollUltimateCastChance()` 内部会：
   - 先算概率 `GetUltimateCastChance()`
   - 再安排下一次判定时间 `ScheduleNextUltimateAttempt()`
   - 最后用随机数决定这次放不放

也就是说：

- 满足条件 ≠ 必放
- 当前实现已经是：满足条件后，每次判定有一定几率释放

### 2.3 概率会动态提高

当前释放概率不是固定值，而是由这些因素叠加：

- 基础概率
- 超出条件阈值的收益
- fallback 是否已经生效
- 某些 secondaryCondition 带来的额外优先级

所以当前系统的设计思路本身没问题，问题更多出在“同步性”。

---

## 3. 当前存在的问题

### 现象

现在会出现一个现象：

- 第一次接敌时，因为双方站位很近
- 很多英雄会在差不多同一时间满足开大条件
- 然后在相近的时间片里一起进入概率判定
- 于是会出现“一波人一起开大”的情况

### 根本原因

问题的核心不是“有概率”这件事，而是：

**很多英雄第一次满足条件的时刻过于接近。**

虽然现在已经有：

- 统一开局保护期
- 每轮判定间隔
- 少量随机 jitter

但这个 jitter 只有最多 `0.5 秒`，而第一次接敌时大量英雄会在同一段时间持续满足条件，所以仍然容易同步开大。

因此真正的问题是：

> 现在的系统是“满足条件后立即进行概率判定”，而不是“满足条件后先进入各自不同的准备窗口，再进行概率判定”。

---

## 4. 不推荐只做的改法

### 4.1 只降低基础概率

不推荐只把 `UltimateBaseReleaseChance` 从 `0.3` 降低。

原因：

- 这只能减少总释放量
- 不能真正解决“同步进入判定窗口”的问题
- 可能会导致整体太憋大，影响节奏

### 4.2 只增大 jitter

不推荐只把 `UltimateDecisionJitterSeconds` 从 `0.5` 调大。

原因：

- 会有缓解
- 但如果很多英雄在同一段时间持续满足条件，后续几轮还是可能重叠
- 只能部分缓解，不能从机制上打散第一次接敌时的集体开大

### 4.3 做全局硬锁

例如：

- “有人开大后，全场其他人 2 秒不能开大”

不推荐。

原因：

- 太硬
- 会让节奏显得不自然
- 容易出现“明明是很好的开大时机，但因为系统锁死而不能放”的违和感

---

## 5. 推荐改法（最重要）

### 5.1 增加“首次满足条件后的随机准备期”

这是最推荐的方案。

#### 当前问题点

当前逻辑是：

- 条件满足
- 立即掷概率

#### 推荐改成

- 条件首次满足
- 不立刻掷概率
- 先进入一个“准备开大状态”
- 每个英雄获得一个独立随机准备时间，例如 `0.8 ~ 2.0 秒`
- 只有这个准备时间到了之后，才真正允许进行概率判定

#### 这样会带来的效果

即使 5 个英雄同时在第一次接敌时满足条件：

- 他们不会立刻同时抽签
- 而是先各自进入不同长度的准备状态
- 于是开大的时间自然就被拉开了

#### 这是解决问题的核心

这比单纯调低概率、调大 jitter 更有效，因为它直接打散了“第一次满足条件就立刻开大”的同步性。

---

### 5.2 概率改成“连续满足越久，越容易释放”

当前的基础概率对第一次满足条件来说仍然偏高。

建议改成：

- 第一次满足条件：基础概率更低，例如 `0.12 ~ 0.18`
- 如果连续多轮仍满足条件：概率逐步上升
- fallback 生效后：继续提高

#### 推荐思路

例如：

- 第一次准备期结束后：`12%`
- 连续满足 1 轮：`+10%`
- 连续满足 2 轮：再 `+10%`
- 仍可叠加：
  - 超额收益 bonus
  - fallback bonus
  - secondary priority bonus

#### 好处

这样英雄会更像“观察机会后再决定”，而不是“刚一达标就抽一次奖”。

---

### 5.3 增加“同阵营开大拥塞抑制”

如果还想进一步防止扎堆，可以再加一层：

- 某个英雄刚成功释放了大招
- 在接下来 `5 秒` 内，同阵营其他英雄的大招判定概率大幅下降
- 这种抑制只作用于友方侧，不影响敌方侧
- 实现上可以：
  - 要么降低同阵营其他英雄的开大概率
  - 要么直接让同阵营其他英雄延后一轮判定

#### 推荐特点

- 不做全局锁
- 不影响敌方阵营
- 更像“同一边团战节奏自动错峰”

#### 例子

- 时间窗：`5 秒`
- 同阵营概率乘数：`0.2 ~ 0.4`

这个适合作为第二层保险，但不应该替代“准备期机制”。

---

## 6. 推荐的组合方案

如果目标是：

- 避免第一次接敌集体开大
- 同时保留随机性
- 又不让大招憋太久不放

最推荐的组合是：

### A. 首次满足条件后，不立刻释放

先进入 `0.8 ~ 2.0 秒` 的个人随机准备期。

### B. 准备期结束后的首次释放基础概率降低

比如改成 `0.12 ~ 0.18`。

### C. 连续满足条件则概率递增

例如每持续一轮满足条件，概率额外增加 `0.08 ~ 0.12`。

### D. 同阵营刚有人开过大时，做友方侧抑制

例如在 `5 秒` 内，同阵营已有英雄开大，则：

- 本轮概率乘 `0.2 ~ 0.4`
- 或者直接延后一轮判定

---

## 7. 对当前问题的最核心判断

这次讨论里最重要的一句结论是：

> 现在的问题，不是“概率太高”，而是“条件满足的时机同步了，而概率判定又发生在满足条件的同一时刻”。

所以真正该改的不是单个数值，而是：

> 让“满足条件”到“真正允许释放”之间，多一层每个英雄自己的随机准备窗口。

---

## 8. 推荐的最小代码改动方向

如果让另一个 AI 继续改代码，建议优先从下面两个文件入手：

- `game/Assets/Scripts/Heroes/RuntimeHero.cs`
- `game/Assets/Scripts/Battle/BattleSkillSystem.cs`

### 8.1 RuntimeHero 里建议新增的运行时字段

建议新增类似这些字段：

- `HasEnteredUltimateReadyState`
- `UltimateReadySinceTimeSeconds`
- `UltimateCommitNotBeforeTimeSeconds`
- `UltimateConditionStreakCount`

可选再加：

- `LastAllyUltimateSuppressedTimeSeconds`
- `LastObservedAllyUltimateTimeSeconds`

### 8.2 BattleSkillSystem 里建议调整的逻辑

重点改 `TryCastUltimate()`，把当前流程：

- 满足条件 -> 立即 RollUltimateCastChance()

改成：

- 满足条件但尚未进入 ready 状态：
  - 进入 ready
  - 生成个人随机准备时间
  - 本轮不释放
- 后续仍满足条件但未到准备完成时刻：
  - 继续等待
- 到了准备完成时刻后：
  - 再调用概率判定
- 若条件中断：
  - 清除 ready 状态
  - streak 清零

### 8.3 概率计算建议调整

重点改 `GetUltimateCastChance()`：

在当前基础上增加：

- streak bonus
- ally ultimate suppression multiplier

并降低第一次满足条件时的基础释放概率。

---

## 9. 给另一个 AI 的直接任务建议

如果要继续修改，可以直接按下面目标做：

### 目标 1
在不重写整个技能系统的前提下，减少第一次接敌时的群体同步开大。

### 目标 2
保留“满足条件后按概率释放”的总体思路，不要改成简单的固定阈值必放。

### 目标 3
优先做最小改动方案：

- 在 `RuntimeHero` 增加大招 ready 状态
- 在 `BattleSkillSystem.TryCastUltimate()` 引入“首次满足条件后的随机准备期”
- 在 `GetUltimateCastChance()` 中加入连续满足递增机制

### 目标 4
如有需要，再额外加入“同阵营刚有人开大时的友方侧抑制”。

---

## 10. 本次讨论的最终推荐结论

最终推荐结论如下：

1. 当前代码已经实现了“满足条件后按概率释放”的大招逻辑。
2. 现阶段最大问题不是缺少随机性，而是第一次满足条件的时机过于同步。
3. 最有效的改法不是单纯调概率，而是：
   - 先进入随机准备期
   - 再做概率释放
4. 最推荐的实现组合是：
   - 首次满足条件后进入 0.8~2.0 秒随机准备期
   - 第一次可释放时基础概率更低
   - 连续满足条件则概率递增
   - 可选增加同阵营开大抑制

---

## 11. 可直接交给 AI 的一句需求描述

可以直接把下面这段发给另一个 AI：

> 请基于 `hero_fight` 当前的 `BattleSkillSystem.TryCastUltimate()` 和 `RuntimeHero` 结构，修改大招释放逻辑。保留“满足条件后按概率释放”的总体方案，但不要让第一次接敌时多个英雄因为同时满足条件而集体开大。优先实现：首次满足条件后先进入每个英雄自己的随机准备期，准备期结束后才允许进行概率释放；同时让连续多轮满足条件时释放概率逐步提高。如需再加第二层保险，请把“某英雄开大后短时间内显著压低同阵营其他英雄的大招判定概率”作为友方侧错峰机制实现，不要影响敌方阵营。尽量采用最小代码改动，不要重写整个技能系统。 
