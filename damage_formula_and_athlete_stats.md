# Teamfight Manager 伤害公式与选手属性数值链分析

本文基于当前目录中的反编译代码整理，目标是回答两件事：

1. 战斗里的伤害到底怎么结算。
2. 选手的面板属性、熟练度、状态、特性，是怎么映射到实战数值的。

关键代码入口：

- `decompiled_scripts/Formula.cs`
- `decompiled_scripts/AttackEffect.cs`
- `decompiled_scripts/MagicEffect.cs`
- `decompiled_scripts/Targetable.cs`
- `decompiled_scripts/Champion.cs`
- `decompiled_scripts/Athlete.cs`
- `decompiled_scripts/AthleteBuffState.cs`
- `decompiled_scripts/ChampionCollectData.cs`
- `decompiled_scripts/ExpValue.cs`
- `decompiled_scripts/ExpParams.cs`
- `decompiled_scripts/GrowthConfig.cs`

## 1. 伤害结算主链

### 1.1 原始伤害

物理/普攻类效果来自 `AttackEffect.Use`：

```text
raw =
  floor(user.Coef * Coef * caster.Attack)
  + Damage
  + floor(target.MaxHp * CoefEnemyHp)
```

对应代码：

- `decompiled_scripts/AttackEffect.cs:83`

法术类效果来自 `MagicEffect.Use`：

```text
raw =
  floor(user.Coef * Coef * caster.Magic)
  + Damage
```

对应代码：

- `decompiled_scripts/MagicEffect.cs:58`

补充：

- `AttackEffect` 和 `MagicEffect` 最后都走同一个 `ApplyDamage`。
- 法术类多目标命中时会先做一次群体衰减：`user.Coef = 1 - 0.1 * (目标数 - 1)`。
- 也就是说法术类群体技能通常是每多打到一个目标，全体伤害再少 10%。

对应代码：

- `decompiled_scripts/MagicEffect.cs:42`
- `decompiled_scripts/MagicEffect.cs:43`

### 1.2 最终扣血

真正扣血在 `Targetable.ApplyDamage`：

```text
effectiveDef =
  ignoreDefence ? 0 : (target.Defence - attacker.DefenceIgnore)

finalDamage =
  ((raw * 100 + 99 + effectiveDef) / max(50, 100 + effectiveDef))
  * max(100 + target.TankMult, 1)
  / 100
```

对应代码：

- `decompiled_scripts/Formula.cs:5`
- `decompiled_scripts/Targetable.cs:407`

几点非常关键：

- `Defence` 同时给物理伤害和法术伤害减伤，没有单独魔抗。
- `DefenceIgnore` 是直接减目标防御。
- 分母最低是 `50`，所以防御再低也不会无限放大伤害。
- 最终伤害会被 `clamp(1, Hp + Shield)`，所以只要原始伤害不是 0，通常至少会打 1。
- 如果原始值 `raw == 0`，游戏会强制令最终伤害为 0。
- 伤害先打护盾，再扣生命。
- `DeathlessState` 会把致死伤害截到“剩 1 点血”。

对应代码：

- `decompiled_scripts/Targetable.cs:408`
- `decompiled_scripts/Targetable.cs:409`
- `decompiled_scripts/Targetable.cs:410`
- `decompiled_scripts/Targetable.cs:414`
- `decompiled_scripts/Targetable.cs:423`

### 1.3 吸血与治疗

吸血：

```text
heal = actualDamage * (self.Vamp + effectVamp) / 100
```

对应代码：

- `decompiled_scripts/Targetable.cs:444`
- `decompiled_scripts/AttackEffect.cs:123`
- `decompiled_scripts/MagicEffect.cs:98`

治疗：

```text
heal =
  round(user.Coef * (Coef * caster.Magic + value)
  * (100 + caster.HealMult) / 100
  * (100 + target.HealedMult) / 100)
```

对应代码：

- `decompiled_scripts/HealEffect.cs:52`
- `decompiled_scripts/HealEffect.cs:54`

## 2. 选手属性如何映射到实战

### 2.1 战斗中真正生效的是 `AthleteBuffState`

英雄绑定选手后，会给英雄挂一个常驻的 `AthleteBuffState`，并把选手缓存成 `AthleteMatch`。

对应代码：

- `decompiled_scripts/Champion.cs:198`
- `decompiled_scripts/Champion.cs:206`

英雄的最终属性来自三部分：

```text
英雄最终属性 = 英雄基础面板(ChampionInfo)
            + 当前补丁(PatchData)
            + 当前所有Buff(AddStatState汇总)
```

对应代码：

- `decompiled_scripts/Champion.cs:31`
- `decompiled_scripts/Champion.cs:124`
- `decompiled_scripts/Champion.cs:126`
- `decompiled_scripts/Champion.cs:128`
- `decompiled_scripts/Champion.cs:130`

### 2.2 选手攻击、选手防御、熟练度的映射

`AthleteBuffState` 的核心映射如下：

```text
熟练度加成 = Athlete.Exps[当前英雄索引]

Attack增量 =
  选手Attack
  + 当前英雄熟练度
  + 选手特性带来的攻击修正
  + 选人奖励/事件奖励

Magic增量 =
  与 Attack 完全相同

MaxHp增量 =
  (选手Defence
  + 当前英雄熟练度
  + 选手特性带来的防御修正
  + 选人奖励/事件奖励) * 5
```

对应代码：

- `decompiled_scripts/AthleteBuffState.cs:15`
- `decompiled_scripts/AthleteBuffState.cs:58`
- `decompiled_scripts/AthleteBuffState.cs:60`
- `decompiled_scripts/AthleteBuffState.cs:63`

因此有三个核心结论：

1. `选手攻击` 同时抬高英雄的 `Attack` 和 `Magic`。
2. `选手防御` 不进入英雄 `Defence`，而是转成 `MaxHp`。
3. `当前英雄熟练度` 同时提供输出和生存：`+1熟练度 = +1 Attack +1 Magic +5 MaxHp`。

这也是为什么选手缓存里的综合值写成：

```text
GetStatSum = Attack + Defence + 2 * 当前英雄熟练度
```

对应代码：

- `decompiled_scripts/AthleteCache.cs:64`

### 2.3 一个容易混淆的点：选手防御不是护甲

英雄本身的减伤防御来自：

```text
Champion.Defence = Patch.Defence + Info.Defence + Buffed.Defence
```

而 `AthleteBuffState` 写入的是：

```text
Defence = 0
MaxHp = (...) * 5
```

对应代码：

- `decompiled_scripts/Champion.cs:128`
- `decompiled_scripts/AthleteBuffState.cs:59`
- `decompiled_scripts/AthleteBuffState.cs:63`

所以：

- 英雄自带的 `Defence` 仍然重要，它决定减伤。
- 选手面板里的 `Defence` 不会直接变成英雄护甲。
- 选手防御本质上更接近“额外生命成长”。

### 2.4 状态值 `Condition` 的影响

`Condition` 的实战影响非常直接：

```text
AttackSpeed加成 = 选手AttackSpeedBuff + floor(Condition / 2.5)
SkillSpeed加成  = 选手SkillSpeedBuff
MoveSpeed加成   = Condition >= 30 时 +1
                = Condition < -30 时 -1
```

对应代码：

- `decompiled_scripts/AthleteBuffState.cs:16`
- `decompiled_scripts/AthleteBuffState.cs:18`
- `decompiled_scripts/AthleteBuffState.cs:20`
- `decompiled_scripts/AthleteBuffState.cs:39`
- `decompiled_scripts/AthleteBuffState.cs:43`

换句话说：

- `Condition` 不直接加攻击和防御。
- 它主要影响攻速和移速。
- 状态很高时节奏会明显快一档，状态很差时则会慢一档。

### 2.5 场上队友还会改变你的状态

如果队内有带 `Moodmaker` 特性的其他选手，场上的 `Condition` 会再额外加成：

```text
实战Condition = clamp(基础Condition + 20 * 其他Moodmaker数量, -50, 50)
```

对应代码：

- `decompiled_scripts/AthleteMatch.cs:21`
- `decompiled_scripts/AthleteMoodmakerProperty.cs:8`

## 3. 攻速、技能速度、移动速度如何影响节奏

### 3.1 攻速与技能速度本质是“缩短帧数”

游戏里用的是：

```text
ApplySpeed(frame, speed) = max(frame * 100 / speed, 1)
```

对应代码：

- `decompiled_scripts/Formula.cs:15`

基础攻击动作使用 `AttackSpeed`，技能使用 `SkillSpeed`：

- `decompiled_scripts/ActionPlayer.cs:24`
- `decompiled_scripts/ActionPlayer.cs:28`
- `decompiled_scripts/ActionPlayer.cs:32`

所以：

- 攻速越高，普攻整套动作越快。
- 技能速度越高，技能前后摇和冷却等待帧越短。

### 3.2 选手相关的速度来源

选手自己能提供或间接提供的速度项主要有：

- `Condition` 影响攻速和移速。
- `Wind` 提供 `AttackSpeed +10`。
- `FastCast` 提供 `SkillSpeed +10`。
- `Distance` 在远程英雄上提供 `AttackSpeed +5`。
- `Magician` 在法师英雄上提供 `SkillSpeed +5`。
- `FastMan/SlowMan` 提供 `MoveSpeed +1/-1`。
- `Safe/Sloth` 满血时提供 `AttackSpeed +5/-5`。
- `Cockroach` 低血时提供 `MoveSpeed +2`。

对应代码：

- `decompiled_scripts/AthleteWindProperty.cs`
- `decompiled_scripts/AthleteFastCastProperty.cs`
- `decompiled_scripts/AthleteDistanceProperty.cs`
- `decompiled_scripts/AthleteMagicianProperty.cs`
- `decompiled_scripts/AthleteFastManProperty.cs`
- `decompiled_scripts/AthleteSlowManProperty.cs`
- `decompiled_scripts/AthleteSafeProperty.cs`
- `decompiled_scripts/AthleteSlothProperty.cs`
- `decompiled_scripts/AthleteCockroachProperty.cs`

## 4. 熟练度、训练、潜力、年龄是怎么生效的

### 4.1 面板值本质上是“经验换算后的等级”

`Attack`、`Defence`、英雄熟练度都不是直接存一个固定数值，而是先存经验，再换算成当前等级。

换算逻辑：

```text
value = floor(((exp - Base) / Mul) ^ (1 / Exp))
needExp(value) = ceil(Mul * value ^ Exp + Base)
```

对应代码：

- `decompiled_scripts/ExpValue.cs:7`
- `decompiled_scripts/ExpValue.cs:9`
- `decompiled_scripts/ExpParams.cs:13`
- `decompiled_scripts/ExpParams.cs:22`

这意味着：

- 训练增加的是经验，不一定立刻涨 1 点面板。
- 后期每涨 1 点需要更多经验。

### 4.2 潜力和年龄不直接进战斗公式

`Potential` 和 `Age` 只影响每天能获得多少经验：

```text
训练经验 = 随机0.9~1.1 * TrainingPointGet[潜力].Get(年龄) * 训练点数
自然成长 = 随机0.9~1.1 * BaseGet[潜力].Get(年龄)
```

对应代码：

- `decompiled_scripts/GrowthConfig.cs:15`
- `decompiled_scripts/GrowthConfig.cs:20`

也就是说：

- 潜力高，不是直接战斗加伤害。
- 潜力高是“成长更快、上限更好”。
- 年龄变化主要通过成长表间接改变长期面板。

### 4.3 日常训练与自然成长

每天处理训练时：

- `Attack` 训练给攻击经验。
- `Defence` 训练给防御经验。
- 指定英雄训练给该英雄熟练度经验。
- `New` 训练累积到 4 点后学会一个新英雄，起始熟练度为 1。
- 每天最后还会给攻击、防御、已会英雄额外发一份自然成长经验。

对应代码：

- `decompiled_scripts/Athlete.cs:406`
- `decompiled_scripts/Athlete.cs:410`
- `decompiled_scripts/Athlete.cs:414`
- `decompiled_scripts/Athlete.cs:418`
- `decompiled_scripts/Athlete.cs:435`
- `decompiled_scripts/Athlete.cs:459`
- `decompiled_scripts/Athlete.cs:463`

## 5. 重要特性对数值的影响

这里只列对数值最有影响的一批。

### 5.1 直接影响输出/生存

- `Clutch`: 决胜局 `Attack +20`、`Defence +20`
- `First`: 比赛还没产生击杀时 `Attack +10`、`Defence +10`
- `Blue` / `Red`: 对应边时 `Attack +5`、`Defence +5`
- `King`: `NeedWin == 3` 时 `Attack +5`、`Defence +5`
- `BloodSmell`: 每击杀 `Attack +2`，上限 `+20`
- `Rage`: 血越低攻击越高，计算为 `缺血百分比 / 5`
- `HeroMind`: 落后时 `+10/+10`，领先时 `-10/-10`
- `WeakMind`: 领先时 `+10/+10`，落后时 `-10/-10`
- `Save`: 替补登场 `+10/+10`，否则 `-10/-10`
- `Dragon`: 开局 `+10/+10`，每 2 秒衰减 1
- `Wait`: 开局 `-10/-10`，每 2 秒回升 1

对应代码：

- `decompiled_scripts/AthleteClutchProperty.cs`
- `decompiled_scripts/AthleteFirstProperty.cs`
- `decompiled_scripts/AthleteBlueProperty.cs`
- `decompiled_scripts/AthleteRedProperty.cs`
- `decompiled_scripts/AthleteKingProperty.cs`
- `decompiled_scripts/AthleteBloodSmellProperty.cs`
- `decompiled_scripts/AthleteRageProperty.cs`
- `decompiled_scripts/AthleteHeroMindProperty.cs`
- `decompiled_scripts/AthleteWeakMindProperty.cs`
- `decompiled_scripts/AthleteSaveProperty.cs`
- `decompiled_scripts/AthleteDragonProperty.cs`
- `decompiled_scripts/AthleteWaitProperty.cs`

### 5.2 直接影响速度、穿防、承伤、治疗

- `Wind`: `AttackSpeed +10`
- `FastCast`: `SkillSpeed +10`
- `Distance`: 远程英雄 `AttackSpeed +5`
- `Magician`: 法师英雄 `SkillSpeed +5`
- `Spear`: `DefenceIgnore +10`
- `IronBody`: `TankMult -5`
- `GlassBody`: `TankMult +5`
- `Healer`: `HealMult +10`
- `KillHeal`: 击杀后回复固定 100 基础值，再吃治疗乘区

对应代码：

- `decompiled_scripts/AthleteWindProperty.cs`
- `decompiled_scripts/AthleteFastCastProperty.cs`
- `decompiled_scripts/AthleteDistanceProperty.cs`
- `decompiled_scripts/AthleteMagicianProperty.cs`
- `decompiled_scripts/AthleteSpearProperty.cs`
- `decompiled_scripts/AthleteIronBodyProperty.cs`
- `decompiled_scripts/AthleteGlassBodyProperty.cs`
- `decompiled_scripts/AthleteHealerProperty.cs`
- `decompiled_scripts/AthleteBuffState.cs:81`

### 5.3 主要改行为，不直接改面板

以下特性主要影响目标选择或行为逻辑，不直接加数值：

- `TargetProperty`: 默认最近目标
- `TargetMinHp`: 优先最低血
- `Together`: 优先最近被攻击目标
- `WinningMind`: 优先生存时间长的目标
- `Chicken`: 己方没人时会逃跑
- `Distraction`: 目标优先级每 5 秒随机洗牌

对应代码：

- `decompiled_scripts/AthleteTargetProperty.cs`
- `decompiled_scripts/AthleteTargetMinHpProperty.cs`
- `decompiled_scripts/AthleteTogetherProperty.cs`
- `decompiled_scripts/AthleteWinningMindProperty.cs`
- `decompiled_scripts/AthleteChickenProperty.cs`
- `decompiled_scripts/AthleteDistractionProperty.cs`
- `decompiled_scripts/ActionTargetBreaker.cs`

## 6. 额外数值机制

### 6.1 选人和事件加成会一起叠加到选手映射里

`AthleteBuffState` 里会额外读取：

- `PickBonusState`
- `StatBonusState`

它们会再往选手的攻击/防御映射上加值。

对应代码：

- `decompiled_scripts/AthleteBuffState.cs:26`
- `decompiled_scripts/AthleteBuffState.cs:31`

### 6.2 加时后的持续伤害

比赛超时后，全场英雄会周期性吃到递增伤害：

```text
value = Random(1..4) + 30 * 超时秒数
```

然后仍然走普通 `ApplyDamage`，所以它不是严格意义的真伤。

对应代码：

- `decompiled_scripts/Game.cs:251`
- `decompiled_scripts/Game.cs:258`

### 6.3 2 人模式下的特殊倍率

在 `Game.PlayerCount == 2` 时，选手映射会被额外放大：

- 大多数英雄的 `Attack/Magic` 映射乘 `3`
- `Swordman` 的 `Attack/Magic` 映射乘 `1.5`
- `MaxHp` 映射乘 `3`

对应代码：

- `decompiled_scripts/AthleteBuffState.cs:34`
- `decompiled_scripts/AthleteBuffState.cs:36`
- `decompiled_scripts/AthleteBuffState.cs:37`

这应该是特殊模式或测试模式专用逻辑，分析正常比赛数值时需要单独注意。

## 7. 一句话总结

如果只记最重要的结论，可以记这四条：

1. 伤害最终统一走 `Formula.Damage(raw, defence, tankMult)`。
2. 选手`攻击`同时加英雄 `Attack` 和 `Magic`。
3. 选手`防御`不加英雄护甲，而是转成 `MaxHp`，比例是 `1防御 = 5生命`。
4. 当前英雄熟练度既加输出也加生存，是选手战力里非常重的一块。

