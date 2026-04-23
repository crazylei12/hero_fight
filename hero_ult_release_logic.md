# Teamfight Manager 英雄大招释放逻辑与决策链分析

本文基于当前目录中的反编译代码整理，目标不是只回答“英雄什么时候开大”，而是把整条逻辑链拆开，让你之后如果要改成更早开、更晚开、更会攒大、更会找角度，能直接对着类和方法下手。

本文重点回答四件事：

1. 英雄每帧是怎么进入“考虑开大”这一步的。
2. 大招什么时候被允许释放。
3. AI 是怎么判断“现在值得开大”的。
4. 如果要改逻辑，应该优先改哪些类。

关键代码入口：

- `decompiled_scripts/Champion.cs`
- `decompiled_scripts/Agent.cs`
- `decompiled_scripts/Actionable.cs`
- `decompiled_scripts/ActionPlayer.cs`
- `decompiled_scripts/ActionCandidate.cs`
- `decompiled_scripts/ActionEffect.cs`
- `decompiled_scripts/ActionState.cs`
- `decompiled_scripts/ActionTargetBreaker.cs`
- `decompiled_scripts/UseCondition.cs`
- `decompiled_scripts/ChampionInfo.cs`
- `decompiled_scripts/ChampionAttribute.cs`

## 1. 先说结论

这个游戏的主动型大招，基本不是“每个英雄一套单独的开大 AI”，而是统一走一套动作候选系统：

1. 每帧进入 `Champion.Run`。
2. `Champion.Run` 调 `Agent.Run`。
3. `Agent.Run` 先尝试被动动作，再尝试普通动作/技能/大招。
4. AI 为每个可用动作找合法目标。
5. AI 给每个候选动作打分。
6. 分数最高且大于 0 的候选会被真正执行。

对主动大招来说，真实效果是：

- 它不是一好就开，而是先要过“动作自身可用时间”与“团队策略等待期”。
- 一旦进入可开状态，只要当前目标合法、收益不是 0，大招通常会因为分数非常高而优先放掉。
- 大招默认只能用一次。
- 大招在“开始施法”那一刻就会被记作已经交掉，不是等效果真正命中才算。

所以如果你主观感受是“这游戏很多英雄一过某个时间点就比较爱乱开大”，这个感觉基本是对的，因为它的大招评分逻辑非常粗粒度，偏向“能打到人就开”，不是特别精细地模拟具体收益。

## 2. 每帧的总调用链

### 2.1 英雄每帧都会进入 Agent

`Champion.Run` 在英雄没死时，每帧都会调用一次 `Agent.Run`。

也就是说，“是否开大”是一个逐帧重算的过程，不是预先排好时间表。

### 2.2 Agent 的动作顺序

`Agent.Run` 的顺序是：

1. `InitAgent()`
2. `RunPassiveAction()`
3. 如果没有被动动作触发，则执行 `RunAction()`
4. 如果当前动作允许重叠，而且单位仍可行动，则同一帧还可能再跑一次 `RunAction()`

这意味着：

- 一些被动动作会优先于普通技能和大招。
- 某些标记型、触发型、反击型机制，并不是放在“普通 AI 决策”里，而是被动动作先截走。
- 如果动作本身允许重叠，单位甚至可以在同一帧连续做两次决策。

可以把它理解成下面这个伪代码：

```text
Champion.Run():
  if alive:
    Agent.Run()

Agent.Run():
  init target policy
  if can trigger passive action:
    use passive action and stop
  else:
    use best normal action
    if current action does not block acting:
      use best normal action again
```

## 3. 动作要先变成“可用”，大招才会进入候选池

### 3.1 所有动作先过 `Actionable.ValidActions`

英雄可考虑的动作来自 `Actionable.ValidActions`。

动作要进入这个列表，至少要满足：

- 单位当前 `CanAct`
- 动作本身 `CanUse`
- 如果是大招，则当前单位不能处于 `LinkType.NotUltLink`
- 如果是移动，则当前必须 `CanMove`

这一步还只是“动作级别”的初筛，还没挑目标。

### 3.2 `CanUse` 不是简单冷却，而是动作前后摇一起算

`ActionPlayer.CanUse` 的判断是：

```text
Game.Frame >= UsableFrame
```

而：

```text
UsableFrame = LastUse + ApplySpeed(EndFrame, Speed) + ApplySpeed(Delay, Speed)
```

几个关键点：

- 普攻用 `AttackSpeed`。
- 技能和大招用 `SkillSpeed`。
- `Formula.ApplySpeed(frame, speed)` 本质上是 `frame * 100 / speed`，速度越高，所需帧数越少。
- 所以选手的 `SkillSpeed` 不只是影响施法动画，也直接影响下一次技能进入可用状态的时间。

这意味着“大招什么时候能开始考虑”，至少受三层因素影响：

1. 该大招自身的 `EndFrame + Delay`
2. 英雄当前 `SkillSpeed`
3. 后面还要说的 `UltWait`

### 3.3 大招默认只能用一次

`ActionPlayer.CanUseTo` 里有一条非常硬的判断：

```text
if Action.IsUlt && (IsUltWaiting(user) || UseCount > 0)
    return false;
```

也就是说：

- 只要这个动作被标记为 `IsUlt`
- 那么一旦 `UseCount > 0`
- 后续无论冷却是否结束、局势是否变化，都不会再用第二次

这就是主动大招“一局一次”的直接来源。

### 3.4 某些召唤物或链接单位根本不允许开大

`Actionable.ValidActions` 里还有：

```text
!action.Action.IsUlt || LinkState != LinkType.NotUltLink
```

这说明：

- 一些链接出来的单位不是“不会想到开大”
- 而是从动作入口层面就不允许把大招加入候选池

所以如果你遇到某些复制体、链接体、特殊召唤体不交大，不一定是 AI 不会用，而可能是设计上直接禁用了。

## 4. 团队策略如何决定“什么时候允许开大”

这一块是最关键的“时机门槛”。

### 4.1 每个大招都有一个 `UltWait`

`ActionPlayer` 在构造时会调用 `SetUltWait()`，它读取当前队伍策略中的 `StrategyUltTiming`，给每个大招生成一个随机等待时间。

注意这里是“每个大招实例各自有一个随机值”，不是全队共用一个固定时间点。

### 4.2 `Timing = None` 不是“随便开”

`SetUltWait(StrategyUltTiming timing)` 的逻辑是：

```text
None  -> 随机在 GameLength 的 1/3 ~ 1/2
Early -> 随机在 GameLength 的 1/6 ~ 1/4
Late  -> 随机在 GameLength 的 7/12 ~ 2/3
```

这个点非常容易误解。

`None` 在代码里不是“没有开大节奏约束”，而是：

- 默认中期再开
- 而且每个英雄会落在一段随机区间里

所以如果不施加“Early”策略，英雄并不会在开局很早就主动交大。

### 4.3 `Combo` 控制的是“跟队友同开还是错开”

`ActionPlayer.IsUltWaiting` 除了看自己是否到 `UltWait`，还会读取队伍最近一次交大时间：

```text
lastUlt = Game.GetLastUlt(user.Target)
```

这个时间是队伍级别的：

- 蓝队用 `BlueLastUlt`
- 红队用 `RedLastUlt`

然后根据 `StrategyUltCombo` 处理。

#### `Combo = None`

直接返回：

```text
Game.Frame < UltWait
```

也就是：

- 时间没到就继续等
- 时间到了就不再等

#### `Combo = Same`

逻辑是：

- 如果自己还没到 `UltWait`
- 但队友最近 1 秒内刚放过大
- 那么允许自己提前跟大

换句话说，`Same` 是一种“允许提前联动爆发”的策略。

更准确地说：

- 平时仍然会等待自己的 `UltWait`
- 但如果队友刚开大，自己可以在 `UltWait` 之前被放行

#### `Combo = Separate`

逻辑是：

- 如果自己还没到 `UltWait`，继续等
- 如果自己已经到 `UltWait`
- 但队友最近 3 秒内刚放过大
- 仍然继续等

也就是：

- 到时间了也不一定马上开
- 还要看是不是刚和队友撞大

这就是“错峰交大”的实现。

### 4.4 策略会在匹配开始后重新下发

`Game.SetStrategy` 在设置蓝红双方策略后，会重新对现有英雄的每个动作调用一次 `action.SetUltWait()`。

这说明：

- `UltWait` 不是写死在英雄数据里
- 它是运行时根据队伍策略重新生成的

如果你以后要改成“战术板直接影响实战开大时机”，这里就是主入口。

## 5. AI 怎么为一个动作找目标

### 5.1 先按动作类型找候选对象

`Agent.GetAction()` 会遍历当前英雄所有 `ValidActions`，然后：

1. 看这个动作第一个 `Effect` 的 `TargetType`
2. 遍历 `Self.Targets`
3. 过滤掉不符合目标类型、条件不满足、分数小于 0 的目标
4. 剩下的目标交给 `ActionTargetBreaker` 选一个最终目标

这里要注意两个实现细节：

- 候选目标是先按“第一个效果”的目标类型来过滤
- 最后只会给每个动作保留一个最终目标，而不是保留多个备选目标

所以一个大招会不会放，很大程度上取决于：

- 第一个效果的目标类型设置得是否合理
- 对应效果的 `CanUseTo` 是否严格
- 目标选择器最终挑中了谁

### 5.2 `Self.Targets` 不是永远等于全场所有人

`Targetable.Targets` 默认来自全场可被选中的单位，但如果单位处在 `TauntState` 下，候选池会被改变。

也就是说：

- 嘲讽、反转目标这类状态
- 会直接影响大招候选目标池

这不是最后一层修正，而是从“能不能把对方纳入候选”开始就改了。

### 5.3 最终目标由 `ActionTargetBreaker` 决定

动作拿到一批合法目标后，会交给 `ActionTargetBreaker.GetTarget()` 选出一个最终目标。

默认支持的策略包括：

- `Nearest`
- `UsedHP`
- `UsedHPAround`
- `Attacked`
- `LiveLong`
- `MinHP`
- `MaxHP`
- `Random`
- `HPRank`
- `AttackRank`
- `Farthest`
- `MinTank`

几个常见理解：

- `Nearest`：最近目标
- `MinHP`：当前血量最低
- `UsedHP`：剩余血量比例最低
- `AttackRank`：更偏向当前输出高的单位，刺客额外加权
- `Attacked`：最近被动作打过的单位
- `MinTank`：累计承伤最低，偏脆皮

### 5.4 选手特性可以改目标偏好

`Agent.InitAgent()` 会读取选手的第一个目标类特性 `FirstTargetProperty()`，让它替换默认的 `EnemyBreaker`。

所以实战中“同一个英雄，换不同选手后大招偏好不同”是可能发生的，而且逻辑不在英雄身上，在选手特性上。

例如：

- `AthleteTargetMinHpProperty` 会改成优先找最低血量目标
- `AthleteWinningMindProperty` 会改成优先找存活时间长的目标
- `AthleteMinDealProperty` 会更偏向当前承伤少的目标

## 6. 大招进入候选后，AI 怎么判断“值不值得现在放”

这一块在 `ActionCandidate.Score`。

### 6.1 普通技能的打分方式

普通动作的逻辑是：

```text
对每个 effect:
  如果这个 effect 对当前目标可用，或者是 MustApply
    把 effect.Score(user, target) 相加
```

也就是说，普通技能的总分是“各效果分数求和”。

### 6.2 许多普通效果的分数其实非常粗

很多常见效果给分都很固定：

- `MoveEffect`：`1`
- `AttackEffect`：`2`
- `MagicEffect`：`2`
- `AddStatEffect`：`2`
- `BlockMoveEffect`：`2`
- `SuppressionEffect`：`2`
- `TauntEffect`：`2`
- `RemoveBuffEffect`：`2`
- `ReviveEffect`：`2`

个别效果会做一点额外区分：

- `HealEffect`：如果目标满血且 `UseOnFull == false`，直接给 `-1`；否则给 `2`
- `ShieldEffect`：给自己套盾是 `2`，给别人套盾是 `3`

这说明普通技能本身的 AI 也不是特别“聪明”，很多时候只是按效果类型粗略打分。

### 6.3 大招的打分方式和普通技能完全不一样

`ActionCandidate.Score` 对大招单独分支处理：

1. 先做若干硬性否决
2. 然后不再“累加普通分”
3. 而是对每个效果取 `UltScore`
4. 最后取这些 `UltScore` 的最大值
5. 只要结果不是 0，就额外加上 `100`

也就是：

```text
ultScore = max(effect.UltScore(...))
if ultScore > 0:
    final = 100 + ultScore
else:
    final = 0
```

这个 `+100` 非常关键。

它意味着：

- 只要大招被认为“有收益”
- 其最终分数几乎总会压过普通技能

所以主动大招一旦解锁，常见表现就是明显更容易被优先放出。

## 7. 大招有哪些硬性否决条件

### 7.1 低血量时会优先保留大招

`ActionCandidate.Score` 对大招有这样一条规则：

```text
如果：
  user 是正常主英雄
  当前血量 <= 40%
  离比赛结束还超过 10 秒
  且没有 DeathlessState
那么：
  直接返回 -1
```

它的实际效果是：

- 血线太低时，英雄会保留大招，不会乱交
- 但如果比赛已经快结束了，这条限制会放开

这不是“看到残血敌人不放大”，而是“自己残血时避免在中前期乱交大”。

### 7.2 仍在 `UltWait` 内，直接不给放

如果 `Action.IsUltWaiting(new ActionTarget(user))` 为真，分数直接返回 `-1`。

所以 `UltWait` 不只是“分数变低”，而是彻底不让这个候选进入可释放状态。

### 7.3 `UseCount > 0` 后彻底失效

前面提过，这条在 `ActionPlayer.CanUseTo` 里处理：

- 用过一次就再也不会作为合法候选

### 7.4 目标、范围、条件不满足也会被否决

即便某个动作理论上已经到了可开时间，仍然可能因为下面这些原因被过滤掉：

- 目标类型不匹配
- 目标不在范围内
- `UseCondition` 不满足
- 效果 `CanUseTo` 返回 false
- 当前已经有相同动作状态

所以“过了等待期”不等于“立刻一定开”，它还得先找到一个合法目标。

## 8. `UltScore` 实际上在算什么

这一块决定了大招 AI 为什么经常显得“偏群体覆盖，而不是精算收益”。

### 8.1 `MoveEffect` 对大招收益记 0

`ActionEffect.UltScore` 里：

- `IgnoreOnUlt` 直接记 `0`
- `MoveEffect` 直接记 `0`

所以如果一个大招包含位移段：

- 位移本身不会让 AI 觉得这招更值
- AI 只看后面真正的伤害、控制、加盾、治疗等效果

### 8.2 持续场、时间型、引导型效果会向内层取分

对下面这些效果：

- `FieldEffect`
- `TimeEffect`
- `ChannelEffect`
- `RunnerEffect`

`UltScore` 不直接用它们自己的分，而是继续往它们的 `OnHitEffect` 里递归，取其中的最大值。

这说明：

- AI 关心的不是“我放了一个场”
- 而是“这个场最终能对多少有效单位产生作用”

### 8.3 大部分情况下，`UltScore` 更像“有效覆盖人数”

对于普通效果，`UltScore` 的核心逻辑是：

1. 根据 `TargetType` 确定要看自己、友军还是敌军
2. 如果效果实现了 `IRange`，就取它的作用范围
3. 在全场单位里找：
   - 没死
   - 阵营符合
   - 目标类型符合
   - 距离/线段检测符合
4. 统计符合条件的单位数量

所以这里更像在回答：

```text
这招现在理论上能作用到多少个有效单位？
```

而不是：

```text
这招现在能打出多少伤害？
能不能斩杀？
能不能阻断关键技能？
会不会治疗溢出？
```

### 8.4 这就是为什么很多主动大招“能中多人就愿意交”

因为它的评估逻辑本身就偏向：

- 多打到几个敌人
- 或多覆盖几个友军
- 就算是“值”

而不是特别细地看当前血线、斩杀窗口、技能联动、敌方位移可能性。

## 9. 真正执行大招时，内部发生了什么

### 9.1 施放前会再做一次合法性校验

`Agent.UseAction()` 在真正调用 `cand.Apply(Self)` 之前，会再次确认：

- `cand.Action.CanUseTo(...)`
- 如果有位移效果，位移是否合法

所以最终执行前还有一道防线，不会因为前面缓存的候选失效就直接空放。

### 9.2 大招在开始施法时就被记为“已经交了”

`ActionPlayer.Apply()` 做了几件事：

1. 发送 `ActionEvent`
2. 给施法者加 `ActionState`
3. 如果是大招，立刻写入 `Game.BlueLastUlt` 或 `Game.RedLastUlt`
4. 执行动作条件的 `Apply`
5. 更新 `LastUse`
6. `UseCount++`

注意这里的大招记录点非常早：

- 还没等效果真正生效
- 只要进入施法，队伍最近一次大招时间就被刷新了
- 同时大招使用次数也已经消耗掉了

### 9.3 真正效果是在 `ActionState` 的 `ApplyFrame` 才落地

动作开始后，并不是立刻触发所有效果，而是在 `ActionState.Run()` 里按每个 `ActionEffect.ApplyFrame` 的时点执行。

也就是说：

- 有前摇的大招，在进入施法和真正命中之间有时间差
- `UltApplyEvent` 也是在实际有效果落地的那一帧才发出

所以你要区分两个时间点：

1. “交大时间”
2. “大招生效时间”

代码里二者不是同一个概念。

### 9.4 前摇被打断后，大招大概率仍然算交掉

`ActionState.OnEnd()` 如果动作不是自然结束，会发一个 `CancelEvent`。

但注意：

- `UseCount` 没有回滚
- `BlueLastUlt / RedLastUlt` 没有回滚
- `LastUse` 也没有回滚

所以从逻辑上说：

- 大招前摇被打断
- 通常仍然算已经交过了

这点很重要，因为它会影响你对“为什么这个英雄明明没打出大招动画，后面也不再开大”的判断。

## 10. 被动大、被动动作、主动大，不是同一套东西

### 10.1 `ChampionInfo` 里有三套相关数据

英雄数据里有三块要分开看：

- `Actions`
- `PassiveActions`
- `Passive`

它们不是一个概念。

### 10.2 `Passive` 是开局或常驻型被动效果

`Game.Run()` 开始时，会先对每个英雄调用一次 `Champion.RunPassive()`。

`Champion.RunPassive()` 会遍历 `Info.Passive`，把这些 `ActionEffect` 直接施加到自己身上。

这一类更像：

- 开局自带状态
- 永久被动
- 常驻开场初始化

### 10.3 `PassiveActions` 是每帧先判定的被动动作

`Agent.RunPassiveAction()` 每帧都会先跑，而且它的优先级高于普通动作。

更关键的是，它在真正施放被动动作前会：

```text
Self.RemoveState<ActionState>()
```

也就是说：

- 某些被动动作可以打断当前正在做的普通动作
- 先触发自己的被动动作

这类机制很像：

- 反击
- 消耗标记后触发
- 某些“看起来像主动释放，但其实由内部条件触发”的技能

### 10.4 `ChampionAttribute.IsPassiveUlt` 更偏 UI 标记

`ChampionAttribute` 里有个 `IsPassiveUlt`，UI 会用它显示“这是被动大招”。

但真正实战里它是否是主动释放、被动效果、还是每帧被动动作，最终还是要看：

- 它到底放在 `Actions`
- 还是 `PassiveActions`
- 还是 `Passive`

不要只看 `IsPassiveUlt` 就判断它的运行方式。

## 11. `UseCondition` 是很多英雄大招“额外门槛”的真正来源

通用大招时机只解决“这个时间段能不能开”，很多具体英雄的大招是否会放，还取决于 `UseCondition`。

### 11.1 `UseCondition` 是额外的硬门槛

动作在找目标时，会检查：

```text
validAction.Action.Conditions.Any(c => !c.CanUseTo(Self, champ))
```

只要某个条件不通过，这个目标就不会进入候选。

### 11.2 常见条件例子

#### `HitCondition`

只有目标当前带控制状态时才允许用。

这类技能会表现成：

- 没控住人不放
- 一旦目标被控，立刻更愿意接上

#### `UsedHpCondition`

只有目标已损失生命值达到一定阈值时才允许用。

这类技能会表现成：

- 对满血或只掉一点血的目标不交
- 更像收割/压低血线后才释放

#### `TargetingTriggerCondition`

必须先有 `TargetingCasterState`，而且已经 `Triggered`，并且目标必须是当初那一个。

这类技能通常对应：

- 先挂标记
- 再触发第二段
- 或者先布置，再引爆

#### `ActionCondition`

当单位本来已经不能自由行动时，只允许在某些指定动作状态之后继续接技能。

这类更像：

- 连段
- 引导后接续
- 特定动作中的第二段

所以如果你看到某些英雄“大招不是时间到了就会放”，很可能不是通用 AI 的问题，而是被这些条件挡住了。

## 12. 一个更接近代码实际的“开大算法”

把前面的逻辑压成伪代码，大概是这样：

```text
每帧:
  if 英雄死亡:
    不处理

  先尝试 PassiveActions
  if 某个 PassiveAction 可用且有合法目标:
    直接执行
    本帧结束

  candidates = []

  对每个 validAction:
    if 当前不能行动:
      跳过
    if 动作自身冷却未到:
      跳过
    if 是大招且已经用过:
      跳过
    if 是大招且还在团队等待期:
      跳过

    找到所有合法目标:
      目标类型必须匹配
      UseCondition 必须满足
      范围判定必须满足
      Score 不能小于 0

    用目标策略选一个最终目标
    生成一个 ActionCandidate

  把 candidates 按 Score 从高到低排序

  从高到低尝试执行:
    只要 Score > 0 且再次校验合法
      立即施放
      停止
```

对于大招，真正关键的分叉在这里：

```text
if 是大招:
  if 自己太残且比赛还早:
    score = -1
  else if 还在 UltWait:
    score = -1
  else:
    score = 100 + max(UltScore of effects)
```

这就是它为什么“过了某个时间窗口后，大招会显著优先于普通技能”的根源。

## 13. 为什么有时看起来像“AI 乱开大”

从代码结构看，常见原因有下面几类。

### 13.1 大招评估偏覆盖，不偏收益精算

它主要看：

- 当前能覆盖多少有效单位

而不是：

- 这波能不能击杀
- 这波是否会溢出治疗
- 对面是否马上位移出圈
- 这波是不是应该留给下一次团战

### 13.2 大招分数有 `+100` 的强抬升

普通技能常见分数大多是 1、2、3 这种量级。

大招一旦有收益，就直接进到 `100 + x`。

这会让它在排序时天然压制普通技能。

### 13.3 目标选择器只保留一个最终目标

AI 并不会为一个动作同时比较多个最终落点的真实收益，而是：

1. 先按策略选一个目标
2. 再对这个目标打分

所以如果选目标这一步本身不够聪明，后面的决策再怎么排序也救不回来。

### 13.4 目标条件经常来自第一个效果

候选池构建很依赖第一个 `Effect` 的 `TargetType` 和范围判断。

如果一个大招的第一个效果设计得比较“宽松”或比较“怪”，AI 的目标候选就会跟你的直觉不一致。

## 14. 如果你要改逻辑，最值得先改哪里

这一节是给你后续动手时用的。

### 14.1 想整体改早开/晚开

优先看：

- `ActionPlayer.SetUltWait`

这里直接决定不同 `Timing` 对应的随机区间。

适合改的方向：

- 把 `None` 改成更早或更晚
- 缩小随机区间，让英雄开大更稳定
- 按英雄类别区分不同等待区间

### 14.2 想改“同开大 / 错开大”

优先看：

- `ActionPlayer.IsUltWaiting`
- `Game.BlueLastUlt`
- `Game.RedLastUlt`

适合改的方向：

- `Same` 的联动窗口不再是 1 秒
- `Separate` 的错峰窗口不再是 3 秒
- 改成按单个英雄而不是按全队共享最近一次大招时间

### 14.3 想改“一局一次”

优先看：

- `ActionPlayer.CanUseTo`

核心限制就是：

```text
Action.IsUlt && UseCount > 0
```

如果你想做：

- 多次开大
- 充能式大招
- 按击杀刷新大招

这一条一定要改。

### 14.4 想让 AI 更会攒大

优先看：

- `ActionCandidate.Score` 的大招分支

现在只有一个很粗的“自己低于 40% 血且比赛还早就不交大”。

如果你想让 AI 更像人，可以在这里加：

- 敌方关键目标血量判断
- 附近敌人数判断
- 我方人数劣势/优势判断
- 是否已经被控
- 是否即将死亡

### 14.5 想让 AI 更会找目标

优先看：

- `Agent.GetAction`
- `ActionTargetBreaker`
- 选手的 `AthleteTargetProperty`

适合改的方向：

- 大招不使用普通目标策略，而使用独立目标策略
- 为 AOE 大招增加“选团最多的人”
- 为治疗大招增加“优先找最低血比友军”

### 14.6 想让 AI 不再乱空大

优先看：

- 各个具体效果的 `CanUseTo`
- `ActionEffect.UltScore`

因为现在 `UltScore` 更像“理论覆盖人数”，不是“真实命中收益”。

适合改的方向：

- 对位移型目标加未来位置预估
- 对范围型技能加更严格的落点预测
- 按实际命中数、击杀概率、治疗缺口重写 `UltScore`

### 14.7 想做英雄专属开大门槛

优先看：

- `UseCondition`

这是最不容易破坏通用框架的方式。

适合做成：

- 目标被控时才放
- 目标低于多少血才放
- 先上标记再放第二段
- 只有自己带某状态时才放

如果只是某一个英雄想更聪明，优先加条件，通常比大改总评分系统更稳。

## 15. 最后给一个工作建议

如果你接下来要真的改这个系统，我建议按下面顺序下手：

1. 先决定你要改的是“时机”还是“目标”还是“收益评估”。
2. 如果只是整体偏早偏晚，先改 `SetUltWait`。
3. 如果是“不要和队友撞大”，先改 `IsUltWaiting`。
4. 如果是“不要一能放就乱放”，先改 `ActionCandidate.Score` 的大招分支。
5. 如果是“这个英雄第二段大招要更聪明”，优先给它加 `UseCondition`。
6. 如果是“这个英雄总选错人”，优先改 `ActionTargetBreaker` 或单独给大招做目标策略。
7. 只有当你确定要全面重做 AI 时，再去大改 `ActionEffect.UltScore`。

原因很简单：

- `SetUltWait` 和 `IsUltWaiting` 改的是全局节奏
- `ActionCandidate.Score` 改的是是否愿意交大
- `ActionTargetBreaker` 改的是找谁开
- `UseCondition` 改的是英雄特例
- `UltScore` 改的是整套大招价值模型，动它的连锁反应最大

## 16. 一句话总结

这个游戏的主动大招逻辑，本质上是：

```text
先过动作可用时间
再过团队等待期和一次性限制
再找一个合法目标
再用一个偏“覆盖人数”的粗粒度模型算收益
只要收益不是 0，就用 100 分量级强行抬高优先级
于是通常会立刻交大
```

所以你之后改大招 AI，最有效的切入点不是“继续找某个隐藏的单英雄脚本”，而是围绕下面四个点动：

- `ActionPlayer.IsUltWaiting`
- `ActionCandidate.Score`
- `ActionTargetBreaker`
- `UseCondition`

这四块基本就是主动大招决策的骨架。
