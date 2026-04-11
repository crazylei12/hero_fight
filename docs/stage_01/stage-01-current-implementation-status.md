# Stage 01 当前实施状态

最后更新：2026-04-10

## 文档用途

这份文档用于记录 `game/` 目录下第一阶段当前已经真正落地的内容。

它不是规划文档，而是实施记录。后续 AI 或开发者接手时，应先读这份文档，快速确认：
- 当前已经实现到哪一步
- 已经生成了哪些场景和数据资产
- 现在能跑通的最小通路是什么
- 还有哪些部分仍然只是占位或未完成

## 当前结论

第一阶段目前已经不再是“只有文档”的状态，而是已经进入了“可运行骨架”阶段。

当前已经完成的核心成果：
- Unity 工程已初始化并可打开
- 第一批基础数据结构已落地
- 最小战斗主循环已落地
- 5v5 基础单位循环已落地
- 技能系统第一版已接入运行时
- 职业 AI 模板第一版已接入运行时
- 最小调试可视化和 HUD 已落地
- 第一批示例英雄、技能和战斗输入资产已自动生成
- `Battle` 场景已经可以直接进入并运行基础自动战斗

当前仍未完成的核心部分：
- 技能系统目前仍是第一版，不是完整版本
- 状态系统目前只接入了第一版属性修正
- HeroSelect 仍是占位场景
- MainMenu 仍是占位场景
- 结果界面、正式 HUD、日志面板仍未完成

## 当前已落地的工程内容

### 1. 数据结构与基础配置

当前已经建立的主要静态数据结构位于：
- `game/Assets/Scripts/Data/`

已落地的关键类型包括：
- `HeroClass`
- `HeroTag`
- `SkillType`
- `SkillTargetType`
- `StatusEffectType`
- `SkillSlotType`
- `TeamSide`
- `BattleEndReason`
- `HeroStatsData`
- `BasicAttackData`
- `StatusEffectData`
- `SkillData`
- `HeroVisualConfig`
- `HeroDefinition`
- `BattleTeamLoadout`
- `BattleInputConfig`
- `BattleResultData`

这些类型已经满足第一阶段最小原型的静态数据承载需求，并且已经可以在 Unity 中作为配置资产使用。

### 2. 最小战斗核心骨架

当前战斗入口和主循环骨架位于：
- `game/Assets/Scripts/Battle/`

已落地的关键运行时模块包括：
- `BattleManager`
- `BattleContext`
- `BattleClock`
- `BattleScoreSystem`
- `BattleRandomService`
- `BattleEndResolver`
- `BattleEventBus`
- `BattleBootstrapper`
- `BattleSimulationSystem`

当前 `BattleManager` 已支持：
- 接收 `BattleInputConfig`
- 创建运行时英雄
- 启动战斗计时
- 记录比分
- 判断常规时间结束
- 同分进入加时
- 下一击杀结束加时
- 产出 `BattleResultData`

### 3. 运行时英雄与基础单位循环

当前运行时英雄位于：
- `game/Assets/Scripts/Heroes/RuntimeHero.cs`

目前已经支持的单位基础能力：
- 出生
- 固定出生点布局
- 自动向敌方区域前进
- 默认按最近敌人索敌
- 进入攻击范围后普攻
- 普攻伤害计算
- 近战瞬时命中普攻
- 远程投射物普攻
- 暴击判定
- 死亡
- 5 秒复活
- 回出生点重新加入战斗
- 基础击杀/死亡/伤害统计

当前基础循环已经形成最小闭环，并且普攻已经覆盖近战与远程两条基础通路。

### 4. 技能系统第一版

当前技能运行时入口位于：
- `game/Assets/Scripts/Battle/BattleSkillSystem.cs`

当前技能系统已经接入到战斗循环中，并在单位每帧逻辑里先于普攻尝试释放。

当前已支持的能力包括：
- 小技能自动释放
- 大招自动释放
- 小技能和大招使用不同的释放判断
- 基于配置的技能 CD
- 基础目标选择策略
- 基础范围技能目标收集
- 单体伤害
- 范围伤害
- 单体治疗
- Buff
- 眩晕

当前大招的第一版差异化规则：
- 伤害型或控制型大招更倾向在影响多个目标时释放
- 普通小技能不要求同样高的价值判断

当前已知限制：
- `Dash` 当前仍按“伤害技能占位”处理，尚未实现真正位移
- 技能动画、特效、投射物表现尚未接入
- 技能系统还没有完整职业策略加成
- 目前仍是最小可运行版本，不是最终技能架构完成态

### 5. 数值结算最小入口

当前已落地的最小数值模块位于：
- `game/Assets/Scripts/Core/CriticalResolver.cs`
- `game/Assets/Scripts/Core/DamageResolver.cs`
- `game/Assets/Scripts/Core/HealResolver.cs`

当前已支持：
- 基础暴击判定
- 基础防御减伤
- 基础普攻伤害结算
- 技能伤害结算复用基础伤害公式
- 技能治疗结算最小入口
- 技能治疗已改为读取运行时攻击力入口

当前尚未支持：
- 更完整的技能专用数值模型
- 更完整的治疗模型
- 更细的 Buff / Debuff 数值修正
- 更完整的状态对属性修正接入

### 6. 状态效果与属性修正第一版

当前已经建立最小运行时状态对象：
- `game/Assets/Scripts/Heroes/RuntimeStatusEffect.cs`

当前状态效果已经开始影响运行时逻辑和部分属性计算，但仍然属于第一版。

当前已接入的效果包括：
- `Stun`：会阻止单位释放技能、移动和普攻
- `AttackSpeedModifier`：已接入攻速修正
- `MoveSpeedModifier`：已接入移速修正
- `AttackPowerModifier`：已接入攻击修正
- `DefenseModifier`：已接入防御修正
- `MaxHealthModifier`：已接入最大生命值修正
- `CriticalChanceModifier`：已接入暴击率修正
- `CriticalDamageModifier`：已接入暴击伤害修正
- `AttackRangeModifier`：已接入攻击距离修正
- `HealOverTime`：已接入持续治疗

说明：
- 当前状态系统已经不再只是静态配置占位
- 但它还不是完整的统一状态系统终版
- 当前修正已经覆盖生命值、攻击、防御、攻速、移速、暴击率、暴击伤害、攻击距离等基础属性入口
- 更复杂的状态叠层规则、驱散规则和更多控制类型仍未实现

### 7. 职业 AI 模板第一版

当前职业行为入口位于：
- `game/Assets/Scripts/Battle/BattleAiDirector.cs`

当前已接入的职业差异包括：
- 战士：默认均衡近战接敌
- 法师：更倾向选择敌方密集区域作为高价值目标
- 刺客：优先寻找后排脆皮与低血目标
- 坦克：默认更稳定地承担前排接敌行为
- 辅助：空闲前进更保守，技能更偏向寻找友方低血目标
- 射手：更倾向攻击低血目标，并维持偏后排输出距离

当前这些差异已经影响：
- 默认索敌
- 空闲推进方向
- 交战期理想攻击距离
- 技能释放目标选择

说明：
- 当前职业 AI 仍然是第一版模板，不是完整状态机
- 但它已经让 6 个模板英雄开始出现可观察的行为差异，而不是完全同一套通用逻辑

### 8. 战斗表现层与调试观察通路

当前已经不再只是“调试可视化”，而是开始收束为 `正式战斗表现层 + 可选调试叠层` 的结构。

关键脚本包括：
- `game/Assets/Scripts/UI/BattleView.cs`
- `game/Assets/Scripts/UI/BattleHud.cs`
- `game/Assets/Scripts/UI/BattleDebugHud.cs`
- `game/Assets/Scripts/UI/BattleDebugView.cs`
- `game/Assets/Scripts/Battle/BattleDebugSceneBootstrap.cs`
- `game/Assets/Scripts/Battle/BattleDebugLogForwarder.cs`

当前已支持：
- 自动生成平面竞技场背景与正交相机
- 用平面 token 或 2D 角色 prefab 显示双方单位
- 用平面 sprite 显示远程普攻投射物与技能范围
- 顶部显示正式战斗 HUD
- 正式 HUD 显示时间、比分与结束结果
- 可选调试叠层显示单位数量、双方单位状态与事件日志
- 显示双方单位当前血量、死亡状态、目标、K/D
- 显示最近的关键战斗事件日志
- Console 输出关键战斗日志

当前结构提醒：
- 当前表现层已经能消费英雄 prefab、投射物 prefab 与部分技能区域特效 prefab
- 但后续新增英雄时，不应继续把越来越多按 `heroId` 或 `skillId` 分支的复杂表现特例堆进 `BattleView`
- 后续应优先遵循 `docs/planning/stage-01-hero-spec-decisions.md` 中新增的“英雄内容归属固定规则”与“复杂表现特例的固定落点”

### 9. 一键生成示例内容

当前已提供编辑器工具：
- `game/Assets/Scripts/Editor/Stage01SampleContentBuilder.cs`

它提供菜单入口：
- `Fight -> Stage 01 -> Generate Demo Content`

当前该工具会自动生成：
- 示例技能资产
- 示例英雄资产
- 示例战斗输入资产
- `MainMenu` 占位场景
- `HeroSelect` 占位场景
- `Battle` 可运行场景
- Build Settings 中的场景列表

## 当前已生成的资源

### 场景

当前已生成场景：
- `game/Assets/Scenes/MainMenu.unity`
- `game/Assets/Scenes/HeroSelect.unity`
- `game/Assets/Scenes/Battle.unity`

说明：
- `Battle.unity` 可运行
- `MainMenu.unity` 是占位
- `HeroSelect.unity` 是占位

### 示例英雄资产

当前已生成示例英雄：
- `Bladeguard`
- `火焰法师（FIREMAGE）`
- `Shadowstep`
- `Ironwall`
- `Sunpriest`
- `Longshot`

资源位置：
- `game/Assets/Data/Stage01Demo/Heroes/<heroId>/`

### 示例技能资产

当前已生成 12 个示例技能资产，位于：
- `game/Assets/Data/Stage01Demo/Skills/<heroId>/`

这些技能资产当前已经接入第一版运行时技能系统。

### 示例战斗输入

当前默认示例战斗输入位于：
- `game/Assets/Data/Stage01Demo/Battles/Stage01DemoBattleInput.asset`

并额外提供了 Resources 回退路径，供 `Battle` 场景自动加载：
- `Resources/Stage01Demo/Stage01DemoBattleInput`

说明：
- 即使场景里没有手动拖引用，`BattleDebugSceneBootstrap` 也会优先尝试从 Resources 自动加载默认战斗输入。

## 当前可运行通路

当前已经能跑通的最小通路是：

1. 打开 `game` Unity 工程
2. 执行 `Fight -> Stage 01 -> Generate Demo Content`
3. 打开 `game/Assets/Scenes/Battle.unity`
4. 点击 Play
5. 进入基础自动战斗调试场景

当前另一个专门用于属性验证的最小通路是：

1. 打开 `game` Unity 工程
2. 执行 `Fight -> Stage 01 -> Generate Demo Content`
3. 打开 `game/Assets/Scenes/BattleBasicAttackOnly.unity`
4. 点击 Play
5. 进入关闭技能后的纯普攻验证场景

当前可观察到的结果包括：
- 双方单位生成
- 自动前进
- 自动索敌
- 普攻交战
- 小技能自动释放
- 大招自动释放
- 治疗生效
- 眩晕生效
- Buff 状态挂载
- 攻速/移速/攻击/防御修正开始影响运行时
- 刺客、法师、辅助、射手已出现基础目标与站位差异
- 击杀计分
- 死亡与复活
- 常规时间结束
- 同分进入加时
- HUD 与调试日志刷新

在 `BattleBasicAttackOnly.unity` 中，当前可额外稳定观察：
- 所有英雄只进行移动、索敌与普攻
- 技能释放不会干扰基础属性验证
- 近战瞬时命中与远程投射物两条普攻通路可单独观察
- 生命、攻击、防御、攻速、移速、暴击率、暴击伤害、攻击距离会持续影响纯普攻战斗结果

## 当前与规划文档的对照

### 已满足的部分

当前已经部分满足或基本满足以下阶段一要求：
- 工程已初始化
- 基础目录结构已建立
- 基础场景已建立
- 核心数据结构已建立
- 战斗输入与结果对象已建立
- 战斗管理器已建立
- 运行时英雄实例已建立
- 基础单位循环已建立
- 技能系统第一版已接入
- 职业 AI 模板第一版已接入
- 最小日志通路已建立
- 第一批示例英雄和技能资产已建立

### 尚未满足的部分

当前仍未满足或仅部分满足以下阶段一要求：
- 技能系统尚未达到完整版本
- 大招释放逻辑仍然只是第一版规则
- 状态系统尚未完整覆盖所有属性和行为修正
- 职业 AI 尚未升级为更明确的职业状态机
- HeroSelect 未真正可用
- 结果 UI 未建立
- 正式战斗日志面板未建立

## 本轮实施中处理过的问题

本轮已经处理过以下实际问题：

### 1. Debug View 使用固定 URP Shader 导致报错

问题：
- 项目当前不保证使用 URP
- 直接 `Shader.Find("Universal Render Pipeline/Lit")` 可能返回空
- 会触发 `ArgumentNullException: Value cannot be null. Parameter name: shader`

处理：
- 将调试材质逻辑改为多级回退
- 优先尝试 `Standard`
- 再尝试 `Universal Render Pipeline/Lit`
- 再尝试其他可用 shader

### 2. Battle 场景初始配置引用为空

问题：
- 生成场景后，`BattleRoot` 上的 `BattleInputConfig` 可能为空
- 导致 HUD 一直停留在 “Waiting for battle start...”

处理：
- 在 `BattleDebugSceneBootstrap` 中增加 Resources 回退加载逻辑
- 在生成器中同步生成默认 `BattleInputConfig` 的 Resources 版本

## 当前限制与注意事项

- 当前战斗已经进入“普攻 + 技能第一版 + 调试场景”阶段，但仍不是完整可玩版本。
- 当前职业差异已开始来自技能类型和职业 AI 第一版，但仍未建立完整职业状态机。
- `Battle` 场景当前已经开始承载正式战斗表现骨架，但仍保留调试入口与调试叠层。
- 部分场景和 UI 仍然是占位，不能误判为第一阶段已经完成。

## 建议的下一步

当前最合理的后续顺序是：

1. 细化小技能与大招释放逻辑
2. 补更多状态效果与技能效果类型
3. 把职业 AI 第一版继续往职业状态机推进
4. 再推进 HeroSelect 和结果 UI
5. 补结果展示与更可读的日志面板

原因：
- 现在基础单位循环、技能第一版、职业 AI 第一版和调试通路都已经有了
- 下一阶段最值得做的是把当前“能跑”的技能系统继续变成“更符合职业定位”的技能系统
- 在职业逻辑和技能行为稳定前，先做复杂 UI 的收益仍然不高

## 给后续 AI 的接手说明

如果后续 AI 从这里继续推进，应默认基于当前工程状态工作，不要重复从零搭骨架。

优先理解以下事实：
- `Battle.unity` 已经能跑基础自动战斗
- 当前主循环核心依赖 `BattleManager + BattleSimulationSystem + RuntimeHero`
- 当前调试观察入口已经存在
- 当前技能系统已经接入第一版运行时
- 当前职业 AI 已接入第一版模板
- 当前真正缺失的是“技能与状态继续补强 + UI 通路”，而不是再建一轮基础目录或空壳场景

一句话总结：
当前第一阶段已经完成“可运行基础战斗骨架 + 技能第一版 + 职业 AI 第一版 + 调试场景 + 示例内容生成”，下一步应继续补强技能、状态与职业逻辑，而不是回头重复搭工程。
