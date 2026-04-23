# 第一阶段离线战斗模拟与批量调数方案

最后更新：2026-04-23

## 文档用途

这份文档用于定义第一阶段中“不开 Unity 实际窗口，也能真实跑一场或多场战斗并导出结果数据”的正式方案。

它主要解决以下问题：
- 如何在不打开表现层窗口的情况下运行真实战斗逻辑
- 如何在项目内提供一个可直接执行的入口，而不是依赖手工点 Play
- 如何一次连续跑 `1` 场、`100` 场或更多场战斗
- 如何导出足够细的对局和英雄数据，服务数值调整

这份文档是后续编码 AI 实现离线模拟入口、批量调数工具和导出格式的直接依据。

## 背景与本轮新增需求

当前项目已经具备可运行的场景战斗主通路，但数值调整仍然主要依赖：
- 打开 Unity
- 进入场景
- 运行单局
- 观察 HUD 或导出单局日志

这条通路适合验证玩法是否正常，不适合高频调数。

本轮明确新增的正式需求是：
- 用户希望在 `不打开 Unity 实际窗口` 的情况下运行一场真实战斗
- 用户希望在项目内有一个 `脚本、命令入口或等价工具`，执行后即可完成战斗并记录数据
- 用户希望支持 `批量` 运行，例如连续跑 `100` 场
- 用户希望能拿到 `每个英雄的各项统计数据`，作为后续平衡调整依据
- 用户希望主结果文件尽量精简，不保留批量运行中的逐场明细，只保留最终英雄场均数据

## 核心结论

第一阶段在这件事上的正式结论如下：

- 第一版应采用 `Unity batchmode + -nographics + 项目内 bat 脚本入口` 作为离线模拟方案
- 第一版 `不直接新做一套独立于 Unity 的战斗引擎`
- 第一版 `不优先做独立 exe`，而是先做仓库内可直接执行的 bat 脚本入口
- 场景内可视战斗与离线批量模拟必须 `复用同一套战斗运行时逻辑`
- 离线模式必须支持：
  - 单场模拟
  - 批量模拟
  - 固定 seed
  - 单局内全局唯一英雄校验
  - 导出每英雄统计
  - 输出单个 AI 易读主结果文件
  - 可选导出完整事件日志

补充结论：
- 以后如果用户强烈希望有双击即可运行的 `.exe` 包装器，可以在脚本方案稳定后再加
- 但在第一版里，`bat 脚本入口 + Unity batchmode` 是最小成本、最不容易和现有战斗逻辑漂移的正式方案

## 为什么第一版不直接做独立 exe

第一版不直接做独立 exe 的原因不是“做不到”，而是当前阶段不值得优先这么做。

原因如下：
- 当前战斗数据大量依赖 Unity 资产和 `ScriptableObject`
- 当前运行时逻辑大量使用 Unity 类型，例如 `Vector3`、`Mathf`
- 如果单独再做一套 Unity 外部 exe 读取和执行逻辑，容易复制出第二套战斗核心
- 一旦出现“场景里打一套、离线 exe 又打一套”，后续调数会非常难对齐

因此第一版应优先保证：
- `规则只维护一套`
- `离线入口只是驱动方式不同`
- `不是重新实现一份战斗`

## 目标与非目标

### 当前目标

第一阶段这套离线模拟能力的目标是：
- 在无窗口模式下，真实推进现有战斗规则
- 复用现有英雄、技能、状态、AI、伤害、治疗、复活、加时逻辑
- 用命令入口直接跑单场或批量战斗
- 产出适合数值分析的结构化数据

### 当前明确不做

第一版明确不做：
- 完整回放系统
- 脱离 Unity 运行时的独立战斗引擎
- 为离线模式单独维护第二套配置格式
- 在第一版就上复杂数据库或在线分析平台
- 在第一版就并行跑多进程多局模拟

说明：
- 第一版重点是先把“单进程、稳定、可复现、可导出”做对
- 后续如果真有性能瓶颈，再考虑并行化或独立打包

## 功能需求

### 1. 运行入口需求

项目内必须提供一个用户可直接执行的入口。

第一版建议形态：
- 仓库脚本：`tools/run_stage01_offline_sim.bat`

补充约束：
- 由于目标机器上可能存在 PowerShell 执行问题，第一版用户主入口不采用 `ps1`
- 如确有需要，后续可以在内部继续复用其他脚本，但用户直接执行入口应保持为 `.bat`

该入口应允许：
- 指定运行模式
- 指定战斗输入资产
- 指定英雄目录或英雄池资产
- 指定运行场次
- 指定 seed 起点
- 指定输出目录
- 指定是否导出完整事件日志

第一版建议至少支持两种模式：
- `FixedInput`
  - 面向单场复现或固定阵容验证
  - 输入为一份现成 `BattleInputConfig`
- `RandomCatalog`
  - 面向批量调数
  - 输入为英雄目录 / HeroCatalog，程序自己随机抽取双方阵容

### 2. 单场模拟需求

至少应支持：
- 输入一份 `BattleInputConfig`
- 在无窗口模式下完成一场完整战斗
- 产出 `BattleResultData` 等价结果
- 额外导出本场的结构化记录文件

### 3. 批量模拟需求

至少应支持：
- 连续运行 `N` 场
- N 可以是 `100` 这种调数级别的批量
- 每场使用不同 seed
- 每场重新初始化上下文，不复用上一场残留运行时状态
- 批量模式默认由程序 `自动随机选择英雄`
- 批量结束后输出总表和汇总结果

第一版对批量模式的推荐行为是：
- 默认使用 `RandomCatalog`
- 从英雄池中为每一场重新抽取蓝红双方阵容
- 不要求用户手工准备 `100` 份不同的 `BattleInputConfig`

推荐输入来源：
- `game/Assets/Resources/Stage01Demo/Stage01HeroCatalog.asset`
- 或后续显式指定的其他 `HeroCatalogData`

### 4. 单局唯一英雄需求

离线模拟工具在第一版里应额外遵守以下阵容生成与校验规则：
- 同一场战斗内，一个英雄只能出现一次
- 蓝红双方之间不允许重复选择同一个英雄
- 批量模式下，阵容应通过 `无放回抽取` 生成，而不是先抽完再事后去重
- 如果英雄池中可用英雄总数不足以支撑一场合法对战，离线模拟应直接报错终止

对于不同运行模式：

- `RandomCatalog`
  - 程序负责随机选择英雄
  - 在选择阶段就必须保证全局无重复
  - 不应出现“本场抽完后才发现重复，再临时替换或硬跳过”的隐式补丁逻辑

- `FixedInput`
  - 仅用于单场复现、对照验证或指定阵容实验
  - 这时如果输入资产中存在重复 heroId，离线模拟应默认 `直接报错并终止`，而不是静默继续运行

说明：
- 这条要求是 `离线模拟工具` 的正式规则，目的是提高调数样本质量
- 它不等于立即修改整个第一阶段主流程的选人规则
- 也就是说，当前 `HeroSelect / Battle` 主流程是否允许镜像英雄，仍按现有战斗规则文档处理；离线模拟工具可以先更严格

如果后续批量模式需要加入更多采样控制：
- 应在“无放回抽取”这个前提上再扩展
- 例如白名单、黑名单、职业配额或镜像禁用开关
- 不要退回到“允许重复，再靠后处理修正”的策略

### 5. 数据导出需求

第一版应有一个 `单个主结果文件` 作为正式交付物，优先保证 AI 能直接读取和总结。

推荐格式：
- `格式化 JSON`

不建议第一版把主结果拆成多份 CSV 作为唯一输出，因为：
- 用户当前明确希望最终结果落成 `一个文件`
- AI 读取单个结构化 JSON 更直接
- 运行信息和按职业分组的英雄汇总可以放在同一份文件中
- 逐场明细默认不进主结果文件，避免批量调数时文件体积和噪音快速膨胀

该主结果文件至少应包含两层内容：

- `runMeta`
- `heroAggregatesByClass`

其中：

`runMeta` 至少包含：
- `generatedAt`
- `selectionMode`
- `inputAssetPath`
- `heroCatalogPath`
- `matchCount`
- `completedMatchCount`
- `seedStart`
- `fixedDeltaTime`
- `exportFullLogs`
- `uniqueHeroValidation`

说明：
- `seedStart` 结合 `matchCount` 即可推出本次批量运行覆盖的 seed 区间
- 如果需要逐场 seed 或完整过程，应开启 `exportFullLogs`
- 第一版主结果文件不再默认保存逐场阵容、逐场胜负和逐场英雄数据

`heroAggregatesByClass` 中每个英雄至少包含：
- `heroId`
- `displayName`
- `heroClass`
- `side`
- `pickCount`
- `winCount`
- `winRate`
- `averageKills`
- `averageDeaths`
- `averageAssists`
- `averageDamageDealt`
- `averageDamageTaken`
- `averageHealingDone`
- `averageShieldingDone`
- `averageActiveSkillCastCount`
- `averageUltimateCastCount`

排序要求：
- 主结果文件中的英雄汇总必须按 `职业分组`
- 职业分组顺序固定为：
  - `Warrior`
  - `Mage`
  - `Assassin`
  - `Tank`
  - `Support`
  - `Marksman`
- 同职业组内，默认按 `heroId` 升序

可读性要求：
- JSON 应采用缩进格式，不要输出压缩成一行的 minified JSON
- 字段命名保持稳定，避免同一含义在不同层里反复改名
- 第一版应优先让 AI 易于总结，而不是追求极致紧凑体积

### 6. 日志需求

离线模式下，完整事件日志应改为 `可选` 导出，不应默认强开。

原因：
- 跑 `100` 场时，如果每场都导出完整文本日志，I/O 会明显放大
- 大多数调数场景先看汇总表，再对异常局补导出完整日志更合理

因此第一版建议：
- 默认输出结构化统计
- 仅在显式开关开启时输出逐场事件日志

## 架构要求

### 1. 必须复用同一套战斗核心

离线模拟与场景内可视战斗必须共用：
- `BattleContext`
- `BattleSimulationSystem`
- `RuntimeHero`
- `BattleSkillSystem`
- `BattleBasicAttackSystem`
- `BattleDamageSystem`
- `StatusEffectSystem`
- `BattleEndResolver`

不允许长期出现：
- 一套给场景战斗
- 一套给离线模拟

### 2. 必须引入独立的纯运行时驱动层

当前 `BattleManager` 仍然是 `MonoBehaviour + Update` 入口。

为了支持无窗口离线模拟，后续应新增一个纯运行时驱动层，建议名称类似：
- `BattleSessionRunner`

它至少应负责：
- 根据输入和 seed 创建一场战斗上下文
- 以指定步长推进战斗
- 判断战斗何时结束
- 产出结果对象

场景内模式和离线模式都应调用它，而不是分别推进。

### 3. BattleManager 的职责应收口为场景适配层

后续 `BattleManager` 应主要承担：
- 场景内接线
- 场景生命周期适配
- 给 HUD / View / SceneBootstrap 提供入口

它不应继续长期独占：
- 战斗初始化逻辑
- 战斗结束组装逻辑
- 唯一的战斗推进逻辑

换句话说：
- `BattleManager` 仍然保留
- 但它应委托给共享的运行时驱动层，而不是自己成为唯一真核心

### 4. 离线模式必须采用固定逻辑步长

离线调数模式不能继续依赖 `Time.deltaTime` 风格的场景帧推进。

第一版应明确采用：
- `固定逻辑步长`

建议默认值：
- `0.05 秒`

原因：
- 更利于结果稳定
- 更利于 seed 复现
- 更利于离线批量统计
- 更避免“帧率变化导致调数结果漂移”

### 5. 离线模式必须显式支持 seed

批量调数要想可复查，必须能记录并复现随机性。

因此第一版应支持：
- `seedStart`
- 每场按固定规则递增或派生 seed
- 主结果文件保留 `seedStart`
- 可选完整日志文件名带出实际 seed

### 6. 日志时间源必须改为战斗时钟

当前 `BattleLogSession` 使用了 `Time.timeSinceLevelLoad` 记录时间。

这在离线模式下不是理想来源。

后续应统一改为优先使用：
- `context.Clock.ElapsedTimeSeconds`

这样：
- 场景内模式和离线模式时间语义一致
- 日志不依赖 Unity 场景加载时间
- 固定步长模拟也更好对齐

## 建议的第一版产物

第一版建议交付以下产物：

### 1. 共享运行时驱动

建议新增：
- `game/Assets/Scripts/Battle/BattleSessionRunner.cs`

### 2. 离线模拟请求与导出对象

建议新增：
- `game/Assets/Scripts/Battle/BattleSimulationRequest.cs`
- `game/Assets/Scripts/Battle/BattleSimulationBatchReport.cs`

### 3. Editor 批处理入口

建议新增：
- `game/Assets/Scripts/Editor/Stage01OfflineSimulationBatch.cs`

职责：
- 解析命令行参数
- 加载输入资产
- 加载 HeroCatalog 或其他英雄池资产
- 调用共享运行时驱动
- 导出报告文件
- 在 batchmode 结束前返回明确成功或失败状态

### 4. 用户可直接执行的仓库脚本

建议新增：
- `tools/run_stage01_offline_sim.bat`

职责：
- 帮用户拼接 Unity batchmode 参数
- 让用户不需要记长命令
- 统一日志输出路径和结果输出路径

## 建议的输出目录结构

当前实现的默认主结果路径是：
- `exports/stage01_offline_simulation/offline_simulation_report.json`

如果用户显式传入：
- `-fightOfflineOutputPath`

则结果会落到该参数指定的文件路径。

当开启完整日志导出时，日志目录会与主结果文件放在同级目录，命名规则为：
- `<report_file_name_without_extension>_logs/`

例如：
- `offline_simulation_report.json`
- `offline_simulation_report_logs/`

其中：

`offline_simulation_report.json` 是第一版唯一的正式主结果文件，内部应同时承载：
- 本次运行元数据
- 每个英雄的聚合统计
- 按职业排序后的分组结果

日志目录仅在开启完整日志时生成，例如：
- `offline_simulation_report_logs/match_0001_seed_1001.txt`

## 用户执行方式目标

第一版应尽量让用户可以直接执行类似下面的命令：

```bat
.\tools\run_stage01_offline_sim.bat -fightOfflineMode FixedInput -fightOfflineCount 1 -fightOfflineInputAssetPath "Assets/Resources/Stage01Demo/Stage01DemoBattleInput.asset"
```

以及：

```bat
.\tools\run_stage01_offline_sim.bat -fightOfflineMode RandomCatalog -fightOfflineCount 100 -fightOfflineSeedStart 1000 -fightOfflineHeroCatalogAssetPath "Assets/Resources/Stage01Demo/Stage01HeroCatalog.asset" -fightOfflineOutputPath "exports/stage01_offline_simulation/test_run_100.json"
```

如需导出逐场完整日志，可额外加：

```bat
-fightOfflineExportFullLogs true
```

说明：
- 这里的“无窗口”指的是 `Unity batchmode + -nographics`
- 不要求用户手工打开编辑器或点 Play
- 对批量调数，推荐默认走 `RandomCatalog`，而不是要求用户手工维护大量输入资产

## 推荐实施顺序

### 第一步：抽离共享战斗运行时驱动

目标：
- 先把“单场完整模拟”从 `BattleManager` 里抽成纯运行时逻辑

最低完成标准：
- 给定 `BattleInputConfig + seed + fixedDeltaTime`
- 可以在不依赖 `MonoBehaviour Update` 的情况下推进到战斗结束
- 正常产出 `BattleResultData`

### 第二步：收口日志时间源和 seed 入口

目标：
- 让离线模拟结果可复现、可对齐

最低完成标准：
- 日志时间使用战斗时钟
- 每场运行 seed 可指定、可记录

### 第三步：建立批量模拟报告结构

目标：
- 先能导出结构化数据，而不是先做漂亮界面

最低完成标准：
- 输出单个 `offline_simulation_report.json`
- 结果内包含运行元数据和按职业分组的英雄汇总
- 主结果文件字段顺序和职业顺序稳定
- 可选完整日志目录能按场次和 seed 稳定落盘

### 第四步：建立 Editor batchmode 命令入口

目标：
- 让项目可以通过命令行直接完成无窗口运行

最低完成标准：
- 能从 Unity batchmode 调起单场模拟
- 能连续跑多场
- 运行失败时返回明确错误

### 第五步：补项目内脚本入口

目标：
- 把复杂命令封装成用户可直接执行的脚本

最低完成标准：
- 用户不需要自己拼 `Unity.exe -batchmode ... -executeMethod ...`
- 直接执行仓库脚本即可

### 第六步：补最小验证

目标：
- 确认离线模式不是“跑得快但结果不对”

最低完成标准：
- 用同一份输入，场景内可视模式与离线固定步长模式在核心结果上大体一致
- 单场、10 场、100 场都能稳定输出报告
- `runMeta`、英雄汇总、日志文件命名中的 seed 信息能稳定对应

## 详细实现步骤

建议按以下顺序编码：

1. 从 `BattleManager` 中抽出“创建上下文、推进、结束、组装结果”的共享逻辑
2. 新增纯运行时的 `BattleSessionRunner`
3. 让 `BattleManager` 改为调用共享驱动，而不是长期自己硬写完整流程
4. 给共享驱动补 `fixedDeltaTime` 和 `seed` 参数
5. 调整 `BattleLogSession` 的时间来源，避免依赖 `Time.timeSinceLevelLoad`
6. 新增批量导出的数据结构和 CSV / JSON 写出逻辑
7. 新增基于 `HeroCatalog` 的随机阵容生成器，并保证单局无放回抽取
8. 新增 `Editor` 下的 batchmode 入口方法
9. 新增仓库 bat 脚本，封装用户执行命令
10. 给 `FixedInput` 模式补重复 heroId 校验
11. 用默认 demo 输入先验证 `1` 场固定阵容模式
12. 再验证 `10` 场随机阵容模式
13. 最后验证 `100` 场输出是否稳定

## 最低验收要求

这套能力正式算落地，至少要满足：

- 不打开 Unity 实际窗口也能跑完一场真实战斗
- 能通过项目 bat 脚本入口直接执行
- 能连续跑 `100` 场
- 批量模式下能由程序自动随机选择双方英雄
- 批量模式生成的每场阵容都满足单局内全局无重复
- `FixedInput` 模式下若出现重复 heroId 能明确报错
- 能导出单个 AI 易读主结果文件
- 主结果文件中包含最终每英雄统计
- 主结果文件中的英雄汇总按职业分组排序
- 能通过 `runMeta` 与可选日志文件还原本次运行的 seed 范围
- 离线模式没有偷偷绕开现有技能、状态、AI、复活、加时逻辑

## 对后续 AI 的约束

后续 AI 在实现这套能力时，必须遵守：

- 不要为了离线模式重写第二套战斗核心
- 不要把离线模式做成“只算数值、不跑真实规则”的近似器
- 不要继续把战斗推进逻辑深埋在 `MonoBehaviour Update` 里不抽离
- 不要让日志时间继续长期依赖 `Time.timeSinceLevelLoad`
- 不要一开始就把重点放到独立 exe 包装，而忽略共享运行时驱动
- 不要把主结果默认拆成一堆零散小文件，导致 AI 读取时还要二次拼接
- 不要把“离线模拟工具禁止重复英雄”误写成整个第一阶段主战斗规则已经全局改动
- 不要要求用户为批量调数手工准备大量不同的 `BattleInputConfig`
- 对批量模式，优先复用现有 `HeroCatalogData` 作为英雄池来源
- 如果实现过程中调整了 `BattleManager`、`BattleResultData`、日志契约或批量导出契约，应同步更新相关文档

## 关联文档

- `docs/planning/project-foundation.md`
- `docs/planning/stage-01-architecture-decisions.md`
- `docs/planning/stage-01-first-implementation-plan.md`
- `docs/planning/stage-01-balance-tuning-reference.md`
- `docs/planning/stage-01-offline-simulation-usage.md`
