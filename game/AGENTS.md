# AGENTS.md

最后更新：2026-04-28

## 文件用途

这份文件用于约束 `game/` 目录下的第一阶段战斗基线与第二阶段 BP / 选手系统编码工作。

它只服务于真正开始实现 Unity 项目时的 AI。只要任务涉及以下内容，就应优先遵循本文件：
- 创建 Unity 工程骨架
- 创建 `Assets/` 目录结构
- 编写 C# 脚本
- 创建 ScriptableObject
- 搭建战斗系统
- 实现英雄、技能、状态、日志、UI
- 实现第二阶段 BP、选手数据、选手特性或选手到战斗输入的桥接

如果任务仍然是总体规划、阶段判断、写规则文档，应回到根目录的 `AGENTS.md`。

## 开始编码前必须阅读

进入第一阶段战斗或第二阶段 BP / 选手系统编码前，至少先阅读以下文档：
- `../docs/planning/project-foundation.md`
- `../docs/planning/stage-01-arena-decisions.md`
- `../docs/planning/stage-01-combat-rules-decisions.md`
- `../docs/planning/stage-01-status-effect-decisions.md`
- `../docs/planning/stage-01-hero-spec-decisions.md`
- `../docs/planning/stage-01-architecture-decisions.md`
- `../docs/planning/stage-01-first-implementation-plan.md`
- `../docs/planning/stage-01-project-bootstrap-checklist.md`
- `../docs/planning/stage-01-plugin-usage-guidelines.md`
- `../docs/planning/stage-02-bp-interface-plan.md`
- `../docs/planning/stage-02-player-system-plan.md`

没有读完这些文档前，不应直接开始乱建脚本或目录。

## 当前编码目标

当前编码目标分为两个层级：
- 第一阶段战斗核心仍是所有后续功能的稳定基线
- 第一批英雄完成后，当前允许进入第二阶段 `真实 BP 界面` 与 `选手系统基础接入`

第一阶段基线目标：
构建最小可运行的 5v5 自动战斗竞技场原型。

完成标准：
- 可以选双方英雄
- 可以进入战斗
- 单位会自动战斗
- 支持普攻、小技能、大招
- 支持死亡、5 秒复活、重新入场
- 支持 60 秒结束比击杀数
- 支持同分进入下一杀加时
- 支持基础日志和结果展示

第二阶段当前目标：
- 实现本地单机真实 Ban/Pick 界面
- 让 BP 流程能从英雄目录中完成禁用与选择
- 让 BP 结果继续输出为统一战斗输入对象
- 保持 BP UI 与战斗核心解耦
- 接入选手基础数据，让 BP 结果表达“选手-英雄绑定”
- 接入选手基础属性和选手特性，让它们通过统一规则轻量影响 BP 展示与战斗表现

## 用户协作方式

当前仓库的默认协作方式固定为：
- 用户主要负责决定方向、确认优先级和验收结果。
- 编码型 AI 默认负责完成几乎全部实际工作，包括：代码编写、资源结构整理、数据资产生成、场景骨架搭建、命令执行、验证与排错。
- 除非确实无法由 AI 在当前环境中替代，否则不要把 Unity 编辑器中的常规操作拆给用户执行。

### 写 AI 检视文档时的额外要求

在本仓库里，`写ai检视文档` 是固定口令，不应被理解成“顺手再写一份 planning 检查清单”。

当用户明确提到 `写ai检视文档` 时，无论本轮主要任务是规划还是编码，都必须把本次对话里已完成的实际工作整理到：
- `../docs/today_work/MMDD.md`

执行规则：
- `MMDD` 使用当前本地日期，例如 `4 月 19 日 -> 0419.md`
- 文件不存在就创建
- 文件已存在则追加本轮新分节，不覆盖当天已有记录
- 只能记录本轮真实完成的修改、验证、限制和遗留问题
- 如果用户没有额外点名 `docs/planning/` 或其他目标位置，不要把这条口令重定向成专项规划文档

每次写入至少包含：
- `本次做了什么`
- `涉及文件`
- `需要检查什么`
- `给评审 AI 的重点`
- `验证情况`

其中：
- `给评审 AI 的重点` 必须明确告诉下一位 AI 该优先检查哪些代码、资源、配置或文档
- 如果本轮修改了 `game/` 下实现，还应明确提醒评审检查编译风险、行为回归、规则是否仍符合 `../docs/planning/`
- 如果本轮没有完成某项验证，必须明确写进 `需要检查什么` 或 `验证情况`

### 只有在以下情况才允许要求用户手动操作

- 必须由用户本人完成的授权、登录、许可确认
- 当前环境无法可靠自动化的 Unity 编辑器点击或拖拽
- 需要用户进行主观判断的内容确认，例如“这个表现是否满意”
- 需要用户亲自观察运行画面或 Console 的实时结果

### 要求用户手动操作时的规则

- 必须先由 AI 尽量完成能自动完成的部分。
- 只有在确认无法继续自动推进时，才向用户提出手动步骤。
- 手动步骤必须尽量少，优先压缩到 1 到 3 步。
- 必须写成对 Unity 新手也能照做的明确指令，不要默认用户理解 Inspector、Hierarchy、Project、Scene 等概念。
- 做完用户步骤后，AI 应立即继续接手后续工作，而不是把整段流程长期转交给用户。

## 第一阶段编码绝对边界

当前明确不做：
- 联机功能本体
- 赛季系统
- 经营系统
- 完整 BP
- 高质量美术和正式音效
- 完整战斗回放
- 大量英雄扩充
- 复杂大厅系统

如果某项工作不直接服务于第一阶段最小原型，默认不做。

## 第二阶段 BP 与选手系统编码边界

第二阶段当前允许做：
- BP 状态机
- Ban / Pick 顺序
- 已禁用、已选择、当前高亮英雄的 UI 状态
- 英雄详情面板、职业筛选、双方阵容栏和禁用区
- BP 结果到 `BattleInputConfig` 或兼容输入对象的桥接
- 选手静态数据结构和示例选手资产
- 蓝红双方 5 个选手槽位
- 选手-英雄绑定结果
- 选手基础属性到战斗运行时副本的统一修正入口
- 选手特性的数据化表达、展示和轻量效果解析

第二阶段当前仍不要做：
- 赛季、经营或完整赛事系统
- 联机 BP、房间、匹配或账号流程
- 把 BP UI 写成战斗核心规则裁判
- 为了 UI 方便改写英雄战斗配置语义
- 选手培养、升级、训练、属性成长或长期状态变化
- 转会、合同、薪资、市场、伤病、疲劳等经营系统

第二阶段 BP 与选手系统的硬边界：
- BP 可以决定“哪些英雄进入本局战斗”
- BP 不决定战斗胜负、伤害、技能、状态、复活或计分
- 禁用与已选状态属于 BP 会话状态，不应污染 `HeroDefinition`
- 阵容确认后仍应通过统一输入对象进入 Battle 场景
- 选手可以影响本局战斗输入或运行时副本，但不修改英雄、技能或选手静态资产
- 选手特性必须走统一解析，不应每个特性都写独立硬编码规则

## 工程结构强制规则

编码时必须按以下分层思考：
- 数据配置层
- 运行时逻辑层
- 表现层
- UI 层

禁止出现以下情况：
- UI 直接负责战斗规则判定
- 特效对象直接决定战斗结果
- HeroDefinition 直接被当作运行时状态容器
- 数值结算散落在各个 MonoBehaviour 中到处复制
- 每个英雄各写一套完全独立且无法复用的逻辑

## 代码结构核心要求

### 必须优先建立的基础模块

优先顺序如下：
1. 数据结构和 ScriptableObject
2. BattleInput / BattleResult
3. BattleManager
4. RuntimeHero
5. BattleEventBus
6. BattleClock / BattleScoreSystem
7. 基础单位循环
8. 数值结算模块
9. 状态系统
10. 技能系统
11. 职业 AI 模板
12. HeroSelect 和最小 HUD

### 必须独立的模块

以下内容不能长期混写：
- 数值结算模块
- 状态系统模块
- AI 决策模块
- 执行模块
- 战斗事件机制
- 随机数服务
- 战斗输入对象
- 战斗结果对象

## 数据规则

- 英雄静态配置与运行时实例必须分离。
- 英雄数据固定拆成：基础数据、战斗逻辑配置、表现资源配置。
- 英雄配置只保留战斗相关字段。
- 英雄 ID 规则固定为：`职业前缀 + 三位序号 + 英文名字`。
- 技能配置必须模板化。
- 普攻配置也必须模板化。
- 标签系统必须保留。
- 特例逻辑允许少量存在，但必须标记，并补说明文档。

## 战斗规则强制要求

编码时不得偏离以下规则：
- 队伍规模是 `5v5`
- 第一阶段只做 `本地单机 1v1`
- 战斗时长常规时间是 `60 秒`
- 英雄死亡后 `5 秒`复活
- 复活后满血回出生点
- 技能 CD 不因复活重置
- 常规时间结束比较击杀数
- 同分进入加时，下一次击杀决胜
- 第一阶段统一伤害体系，不区分物理/法术
- 基础属性至少包含：生命值、攻击力、防御力、攻速、移速、暴击率、暴击伤害、攻击距离

## 英雄实现规则

- 第一批只做六个模板英雄，每个职业一个。
- 每个英雄统一结构：普攻 + 小技能 + 大招。
- 每个职业至少有一个默认 AI 模板。
- 首批英雄必须覆盖：近战、远程投射物、范围伤害、位移、治疗或增益、眩晕、大招范围判断。
- 每个英雄都要有说明文档。
- 每个英雄都要有设计边界说明。

## 小技能与大招的编码要求

- 小技能可以按“CD 好且有合适目标就释放”的方向实现。
- 大招必须有独立释放逻辑，不得直接复用小技能的简单条件。
- 伤害型大招要考虑敌方聚集度。
- 防御型或保护型大招要考虑友方危险程度。

## 日志与调试要求

- 必须保留战斗事件日志。
- 必须预留调试日志。
- 关键事件至少包括：开始、技能释放、伤害、治疗、死亡、复活、比分变化、进入加时、结束。
- 遇到规则异常时，应优先补日志，而不是只靠猜。

## 目录约定

真正创建 Unity 工程后，优先使用以下目录：
- `Assets/Scripts/Core`
- `Assets/Scripts/Battle`
- `Assets/Scripts/Heroes`
- `Assets/Scripts/UI`
- `Assets/Scripts/Data`
- `Assets/Data/Heroes`
- `Assets/Data/Skills`
- `Assets/Prefabs`
- `Assets/Scenes`
- `Assets/Art`
- `Assets/Audio`

新增目录时要克制，尽量服从现有结构。

## 如何打开和进入项目

当 `game/` 目录中已经存在 Unity 工程时，后续 AI 的默认工作根目录应切换到 `game/`。

打开或检查项目时，优先确认以下内容：
- `game/` 下是否存在 `Assets/`
- `game/` 下是否存在 `Packages/`
- `game/` 下是否存在 `ProjectSettings/`
- 是否存在约定的场景目录和脚本目录

如果这些目录还不存在，说明 Unity 工程尚未真正初始化，此时应优先执行项目启动清单，而不是假设工程已经可编译。

## 允许与禁止修改的目录

### 默认允许修改

编码型 AI 默认允许修改：
- `game/Assets/`
- `game/Packages/`
- `game/ProjectSettings/`
- `game/AGENTS.md`
- 与当前实现强相关的 `../docs/planning/` 文档

### 默认不要修改

编码型 AI 默认不要修改：
- 根目录其他无关文件
- `docs/planning/` 中与当前实现无关的大量历史文档
- 用户未要求清理的无关资源或临时文件

### 需要格外谨慎的目录

以下目录如已存在，修改前必须先理解其作用：
- `game/Packages/manifest.json`
- `game/ProjectSettings/`
- `game/Assets/Scenes/`
- `game/Assets/Data/`

不要为了临时跑通而随意破坏包依赖、项目设置或已有数据资源结构。

## 执行命令时的优先规则

- 优先使用非破坏性命令检查当前状态。
- 优先先读目录和关键配置，再决定如何改。
- 如果需要运行 Unity 相关命令或构建命令，先明确这次验证的目标是什么。
- 不要为了“看起来完整”执行与当前阶段无关的重型操作。

## 每轮编码后的提交要求

- 只要本轮任务修改了 `game/` 下的实现代码、工程配置或运行所需数据，完成本轮后必须执行一次 git commit。
- commit 应对应当前这一轮可说明、可回退、可交接的结果，不要把多个无关实现阶段混在一个提交里。
- 如果本轮代码改动同时要求更新 `../docs/planning/` 中的规则、契约或实现说明，应在同一个 commit 内一起提交。
- 提交前先完成本文件要求的最低检查与最小验证，不要在明显未验证的状态下直接提交。
- commit message 应简洁说明本轮目标，优先使用“模块/功能 + 动作”的写法，方便后续追踪。

## 每次改动后的最低检查要求

无论改动大小，完成后至少做与本次改动类型对应的最低检查。

### 数据改动最低检查

如果改的是以下内容：
- ScriptableObject 结构
- 英雄数据
- 技能数据
- 枚举
- 战斗输入输出数据结构

至少检查：
- 数据结构字段命名是否与既有规则一致
- 是否破坏已有配置的可读性和可扩展性
- 是否仍符合第一阶段战斗规则和英雄规范

### 战斗逻辑改动最低检查

如果改的是以下内容：
- BattleManager
- RuntimeHero
- 数值结算
- 状态系统
- 技能系统
- AI 决策

至少检查：
- 逻辑是否仍满足 60 秒、击杀计分、加时决胜规则
- 死亡后 5 秒复活流程是否没有被破坏
- 小技能和大招是否仍保留不同释放逻辑
- 是否绕开了统一事件机制、统一状态系统或统一数值结算

### UI 改动最低检查

如果改的是以下内容：
- HeroSelect
- Battle HUD
- 结果界面
- 日志面板

至少检查：
- UI 是否只读核心状态或提交输入
- UI 是否没有直接接管战斗规则
- 基础流程是否仍然是“选人 -> 开战 -> 结果”

## 最小验证路径

### 数据改动的最小验证路径

- 能否正常创建或读取相关配置
- 新字段是否能被运行时正确消费
- 没有让英雄配置、技能配置或战斗输入对象失去一致性

### 战斗改动的最小验证路径

至少人工验证以下通路：
- 创建一场本地 5v5 战斗
- 单位能出生
- 单位能移动、索敌、普攻
- 至少一个技能可释放
- 单位死亡后 5 秒复活
- 60 秒结束后按击杀数结算
- 同分时能进入下一杀加时

### UI 改动的最小验证路径

至少人工验证以下通路：
- 进入 `HeroSelect`
- 选择双方阵容
- 成功进入 `Battle`
- HUD 能显示时间和比分
- 战斗结束后能进入结果展示

## 出现编译错误时的处理顺序

如果改动后出现编译错误，优先按以下顺序处理：

1. 先修当前改动直接引入的语法错误
2. 再修命名、引用、命名空间和文件路径错误
3. 再修因为数据结构变更导致的连锁错误
4. 最后再处理旧代码中的次要警告或与当前任务弱相关的问题

规则：
- 不要在当前改动已经导致编译失败时继续堆新功能
- 不要为了绕过编译错误而临时破坏既定架构边界
- 如果发现错误暴露出文档与实现冲突，先修正冲突再继续

## 验收优先级

每次提交前，优先确认：
- 能否编译
- 是否符合第一阶段边界
- 是否没有破坏统一输入、结果、事件、状态、数值结算体系
- 是否保留了后续联机与职业状态机扩展边界

如果时间有限，先保编译和核心流程，再谈表现细节。

## 编码风格要求

- 先写最小可运行版本，再做扩展。
- 优先写清晰、稳定、可调试的代码。
- 不要为了“未来可能需要”提前堆大量复杂抽象。
- 但基础边界必须保留，尤其是：输入对象、结果对象、事件、状态、数值结算。
- 如果实现与文档冲突，先修正文档或提出冲突，不要直接偷偷改方向。

## 何时允许特例逻辑

只有在以下条件同时满足时，才允许添加英雄特例逻辑：
- 已尝试使用统一技能系统、统一目标系统、统一状态系统表达
- 确认统一系统暂时无法表达该能力
- 特例逻辑不会破坏现有基础框架
- 特例已在配置和英雄说明文档中标记

## 对后续 AI 的执行指令

如果你现在开始在 `game/` 目录里写代码：
- 先搭项目骨架，不先堆内容
- 先让一场战斗完整跑通，不先做大量英雄
- 先实现统一系统，不先写大量特例
- 每完成一个关键模块，都检查是否符合 `../docs/planning/` 里的既定规则

一句话要求：
先把第一阶段最小原型做对，再谈扩展。

## Minimum verification after each implementation change
After each meaningful implementation change, perform a minimum verification pass before considering the task complete.

Minimum verification rules:
- The Unity project should remain openable without introducing new compile errors.
- Newly added or modified C# scripts must not leave obvious Console compile errors.
- If a new config/data type is added for gameplay use, it should be usable from the intended Unity workflow (for example, serializable in Inspector, creatable as an asset, or assignable from the editor as designed).
- If battle flow code is changed, the Battle scene should still be able to enter the basic combat flow without breaking the current stage-01 scope.
- If scene/bootstrap code is changed, the affected scene should still load and reach its intended placeholder or entry behavior.
- If implementation behavior changes any rule, assumption, or data contract that is documented in `docs/planning/`, update the relevant planning document in the same task.
- Do not mark a task as complete if the code was changed but no verification reasoning was recorded.

When reporting completion, include a short verification note:
- what was changed
- what was checked
- whether any limitation, placeholder, or unverified part remains
