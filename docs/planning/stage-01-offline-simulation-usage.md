# 第一阶段离线战斗模拟使用说明

最后更新：2026-04-23

## 文档用途

这份文档不是讲方案设计，而是讲“现在这套离线模拟工具具体怎么用”。

如果你只是想：
- 不打开 Unity 窗口直接跑一场
- 连续跑 `100` 场拿调数数据
- 看结果文件在哪
- 知道常见报错该怎么排

就看这份文档。

## 入口文件

当前用户入口是：
- `tools/run_stage01_offline_sim.bat`

它内部会调用 Unity batchmode：
- 不打开实际游戏窗口
- 不需要你手工点 Play
- 跑完后输出结果 JSON

另外，当前还提供了一个 Windows 图形启动器：
- `tools/OfflineSimulationLauncher/FightOfflineSimulationLauncher.exe`

它底层仍然调用同一个 `.bat` 和同一个 Unity batchmode 入口，只是把参数选择和进度显示做成了 GUI。

## 使用前准备

运行前默认假设：
- 你在仓库根目录下执行命令
- 本机 Unity 路径可用：
  - `C:\Program Files\Unity 6000.3.13f1\Editor\Unity.exe`

如果你的 Unity 不在这个路径，先在当前命令行里设置：

```bat
set UNITY_EXE=D:\Unity\Editor\Unity.exe
```

然后再运行 `tools\run_stage01_offline_sim.bat`。

建议：
- 先关闭已经打开的同一 Unity 项目，避免编辑器占用项目导致 batchmode 运行变慢或出现锁文件问题

如果你双击 exe 后提示找不到仓库或 bat 入口：
- 确认 exe 仍位于仓库里的 `tools/OfflineSimulationLauncher/` 目录附近
- 如果 exe 丢了，可以执行 `tools\OfflineSimulationLauncher\build.bat` 重新生成

## Windows 启动器怎么用

直接双击：
- `tools/OfflineSimulationLauncher/FightOfflineSimulationLauncher.exe`

打开后可以直接设置：
- `RandomCatalog` 或 `FixedInput`
- 运行多少场
- `seedStart`
- 是否保留每场数据
- 是否额外导出完整日志
- 输出 JSON 路径

启动后界面会实时显示：
- 当前正在第几场
- 总共多少场
- 已完成多少场
- 底部 bat / Unity 返回的运行日志

这套 exe 只是 GUI 包装器，不会绕开现有离线模拟主流程。

## 最快上手

### 1. 什么参数都不写，直接跑默认一局

在仓库根目录打开 `cmd`，执行：

```bat
tools\run_stage01_offline_sim.bat
```

这会使用默认参数：
- 模式：`RandomCatalog`
- 模板输入：`Assets/Resources/Stage01Demo/Stage01DemoBattleInput.asset`
- 英雄池：`Assets/Resources/Stage01Demo/Stage01HeroCatalog.asset`
- 场次：`1`
- `seedStart`：`0`
- 固定步长：`0.05`
- `maxTicks`：`100000`
- 完整日志导出：关闭
- 输出文件：`exports/stage01_offline_simulation/offline_simulation_report.json`

### 2. 跑 100 场随机模拟

```bat
tools\run_stage01_offline_sim.bat -fightOfflineMode RandomCatalog -fightOfflineCount 100 -fightOfflineSeedStart 1000 -fightOfflineOutputPath "exports/stage01_offline_simulation/run_100.json"
```

这是后续调数最常用的方式。

### 3. 用固定阵容跑单场复现

```bat
tools\run_stage01_offline_sim.bat -fightOfflineMode FixedInput -fightOfflineCount 1 -fightOfflineInputAssetPath "Assets/Resources/Stage01Demo/Stage01DemoBattleInput.asset" -fightOfflineOutputPath "exports/stage01_offline_simulation/fixed_input_test.json"
```

适合：
- 复现某个问题
- 对照修改前后差异
- 验证指定阵容

## 两种模式怎么选

### `RandomCatalog`

适合：
- 批量调数
- 自动抽双方阵容
- 连续跑很多局

特点：
- 从 `HeroCatalog` 中无放回抽取
- 单场 10 个英雄全局不重复
- 每场 seed = `seedStart + matchIndex`

注意：
- `RandomCatalog` 仍然会读取一份 `BattleInputConfig` 模板
- 这份模板不是用来指定英雄，而是提供战斗规则和队伍策略默认值，例如：
  - `regulationDurationSeconds`
  - `respawnDelaySeconds`
  - `enableSkills`
  - 蓝红双方团队级大招策略

### `FixedInput`

适合：
- 跑一场指定阵容
- 做问题复现
- 对照验证

特点：
- 直接使用 `BattleInputConfig` 里的蓝红双方英雄
- 如果输入里有重复 `heroId`，会直接报错终止

## 全部参数说明

### `-fightOfflineMode`

可选值：
- `RandomCatalog`
- `FixedInput`

默认：
- `RandomCatalog`

示例：

```bat
-fightOfflineMode RandomCatalog
```

### `-fightOfflineInputAssetPath`

含义：
- `BattleInputConfig` 资产路径

默认：
- `Assets/Resources/Stage01Demo/Stage01DemoBattleInput.asset`

注意：
- 路径必须是 Unity 资产路径，也就是 `Assets/...`
- 不是磁盘绝对路径

### `-fightOfflineHeroCatalogAssetPath`

含义：
- `HeroCatalogData` 资产路径

默认：
- `Assets/Resources/Stage01Demo/Stage01HeroCatalog.asset`

仅在 `RandomCatalog` 下使用。

### `-fightOfflineCount`

含义：
- 本次连续运行多少场

默认：
- `1`

示例：

```bat
-fightOfflineCount 100
```

### `-fightOfflineSeedStart`

含义：
- 第一场使用的 seed

默认：
- `0`

实际规则：
- 第 `0` 场 seed = `seedStart`
- 第 `1` 场 seed = `seedStart + 1`
- 依次递增

用途：
- 复现某一局
- 固定一批样本

### `-fightOfflineFixedDeltaTime`

含义：
- 离线战斗的固定逻辑步长，单位秒

默认：
- `0.05`

一般不需要改。

如果你确实要改：

```bat
-fightOfflineFixedDeltaTime 0.02
```

### `-fightOfflineMaxTicks`

含义：
- 单场最大推进 tick 数

默认：
- `100000`

一般不用改。

如果战斗因为逻辑异常一直不结束，这个值会作为保护上限。

### `-fightOfflineExportFullLogs`

含义：
- 是否导出逐场完整事件日志

可选值：
- `true`
- `false`

默认：
- `false`

示例：

```bat
-fightOfflineExportFullLogs true
```

开启后会额外生成一个日志目录。

### `-fightOfflineIncludeMatchRecords`

含义：
- 是否把逐场结果也写进主结果 JSON

可选值：
- `true`
- `false`

默认：
- `false`

说明：
- `false` 时，主结果 JSON 以最终英雄场均汇总为主
- `true` 时，会额外写出 `matches`，包含逐场胜负、阵容和逐英雄逐场数据

### `-fightOfflineOutputPath`

含义：
- 主结果 JSON 的输出路径

默认：
- `exports/stage01_offline_simulation/offline_simulation_report.json`

支持两种写法：
- 仓库相对路径
- 磁盘绝对路径

仓库相对路径示例：

```bat
-fightOfflineOutputPath "exports/stage01_offline_simulation/run_100.json"
```

绝对路径示例：

```bat
-fightOfflineOutputPath "D:\fight_outputs\run_100.json"
```

## 常用命令模板

### 模板 A：先随手跑一局，确认工具能工作

```bat
tools\run_stage01_offline_sim.bat -fightOfflineOutputPath "exports/stage01_offline_simulation/smoke_test.json"
```

### 模板 B：跑 100 场做平衡观察

```bat
tools\run_stage01_offline_sim.bat -fightOfflineMode RandomCatalog -fightOfflineCount 100 -fightOfflineSeedStart 1000 -fightOfflineOutputPath "exports/stage01_offline_simulation/balance_run_100.json"
```

### 模板 C：跑 1000 场做更稳定的统计

```bat
tools\run_stage01_offline_sim.bat -fightOfflineMode RandomCatalog -fightOfflineCount 1000 -fightOfflineSeedStart 5000 -fightOfflineOutputPath "exports/stage01_offline_simulation/balance_run_1000.json"
```

### 模板 D：导出逐场完整日志

```bat
tools\run_stage01_offline_sim.bat -fightOfflineMode RandomCatalog -fightOfflineCount 20 -fightOfflineSeedStart 2000 -fightOfflineExportFullLogs true -fightOfflineOutputPath "exports/stage01_offline_simulation/run_20_with_logs.json"
```

### 模板 E：保留逐场结果

```bat
tools\run_stage01_offline_sim.bat -fightOfflineMode RandomCatalog -fightOfflineCount 20 -fightOfflineSeedStart 2000 -fightOfflineIncludeMatchRecords true -fightOfflineOutputPath "exports/stage01_offline_simulation/run_20_with_matches.json"
```

### 模板 F：固定阵容复现

```bat
tools\run_stage01_offline_sim.bat -fightOfflineMode FixedInput -fightOfflineInputAssetPath "Assets/Resources/Stage01Demo/Stage01DemoBattleInput.asset" -fightOfflineCount 1 -fightOfflineSeedStart 42 -fightOfflineOutputPath "exports/stage01_offline_simulation/fixed_seed_42.json"
```

## 输出结果怎么看

### 1. Unity 运行日志

每次运行都会写：
- `Temp/stage01_offline_sim_unity.log`

用途：
- 看 batchmode 有没有成功启动
- 看 Unity 编译错误
- 看命令行解析错误

如果 `.bat` 输出：
- `[offline-sim] Failed`

先看这个文件。

### 2. 主结果文件

默认主结果文件：
- `exports/stage01_offline_simulation/offline_simulation_report.json`

核心结构：
- `runMeta`
- `heroAggregatesByClass`

当 `-fightOfflineIncludeMatchRecords true` 时，还会有：
- `matches`

你最常看的部分一般是：

#### `runMeta`

记录本次运行参数，例如：
- 跑了多少场
- 实际完成了多少场
- 用了什么模式
- `seedStart`
- 固定步长是多少
- 是否导出了完整日志
- 是否保留了逐场结果

说明：
- 默认情况下，主结果 JSON 不会保留逐场数据
- 这份文件的目标优先是给你和 AI 直接看最终英雄场均数据
- 如果你还想把逐场过程一起写进主结果 JSON，请加 `-fightOfflineIncludeMatchRecords true`
- 如果你只需要事件过程文本日志，请开启 `-fightOfflineExportFullLogs true`

#### `matches`

只有在 `-fightOfflineIncludeMatchRecords true` 时才会填充。

每场至少会有：
- `matchIndex`
- `seed`
- `winner`
- `endReason`
- `enteredOvertime`
- `elapsedTimeSeconds`
- `blueKills`
- `redKills`
- `blueHeroes`
- `redHeroes`
- `heroStats`

#### `heroAggregatesByClass`

这是批量调数时最重要的部分。

它会按职业分组输出：
- `Warrior`
- `Mage`
- `Assassin`
- `Tank`
- `Support`
- `Marksman`

每个英雄会有聚合统计，例如：
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

### 3. 完整日志目录

只有在 `-fightOfflineExportFullLogs true` 时才会生成。

目录命名规则：
- `<结果文件名去掉扩展名>_logs`

例如：
- 主结果：`exports/stage01_offline_simulation/run_20_with_logs.json`
- 日志目录：`exports/stage01_offline_simulation/run_20_with_logs_logs/`

逐场日志文件示例：
- `match_0001_seed_2000.txt`

## 怎么复现某一局

如果你开了 `-fightOfflineIncludeMatchRecords true`，可以直接从 `matches` 里找目标 seed。

如果你没开这项参数，复现某一局有两种方式：

### 1. 你已经知道目标 seed

最简单，直接单独跑这一局：

```bat
tools\run_stage01_offline_sim.bat -fightOfflineMode RandomCatalog -fightOfflineCount 1 -fightOfflineSeedStart 1037 -fightOfflineOutputPath "exports/stage01_offline_simulation/replay_seed_1037.json"
```

### 2. 你还不知道目标 seed

先在批量运行时开启：

```bat
-fightOfflineExportFullLogs true
```

日志文件名会直接带上 seed，例如：
- `match_0038_seed_1037.txt`

确认 seed 之后，再按上面的单局命令复现。

## 常见报错和处理方法

### 1. 找不到 Unity

如果输出类似：

```text
[offline-sim] Unity executable not found: ...
```

处理方式：
- 先设置 `UNITY_EXE`
- 再重新运行

示例：

```bat
set UNITY_EXE=D:\Unity\Editor\Unity.exe
tools\run_stage01_offline_sim.bat
```

### 2. HeroCatalog 加载失败

如果日志里看到类似：
- `Could not load HeroCatalogData`

通常说明：
- `-fightOfflineHeroCatalogAssetPath` 写错了
- 路径不是 `Assets/...` 形式

正确示例：

```bat
-fightOfflineHeroCatalogAssetPath "Assets/Resources/Stage01Demo/Stage01HeroCatalog.asset"
```

### 3. BattleInput 加载失败

如果日志里看到类似：
- `Could not load BattleInputConfig`

通常说明：
- `-fightOfflineInputAssetPath` 写错了
- 路径不是 Unity 资产路径

### 4. 固定阵容模式提示重复 heroId

这不是 bug，是当前工具的正式规则。

含义：
- 同一场里蓝红双方不能有重复英雄

处理方式：
- 去改那份 `BattleInputConfig`
- 保证单场 10 个英雄全局唯一

### 5. 随机模式提示英雄池不足

如果日志里看到类似：
- `Hero catalog must contain at least 10 unique heroes`

说明：
- 你的 `HeroCatalogData` 里可用且唯一的英雄数不够

处理方式：
- 往 `HeroCatalog` 里补英雄
- 或检查里面是否有重复 `heroId`

### 6. 运行很慢

常见原因：
- 第一次 batchmode 导入和编译脚本，本来就会慢
- 项目里第三方资源较多，Unity 首次批处理启动成本高
- 同时还开着这个项目的 Unity 编辑器

建议：
- 第一次先接受它会慢
- 后续重复跑通常会快一些
- 批量大样本时优先一次性跑，比如 `100` 场、`1000` 场，而不是频繁重复启动很多次

## 推荐使用习惯

### 调数时

建议优先用：
- `RandomCatalog`
- 批量 `100` 场起步
- 固定一个 `seedStart`

这样你改数值前后，可以更容易横向对比。

### 排查问题时

建议优先用：
- `FixedInput`
- `fightOfflineCount 1`
- 明确指定输出文件

如果还需要看详细过程，再加：
- `-fightOfflineExportFullLogs true`

## 当前实现边界

这套工具当前已经能做：
- 无窗口跑真实战斗
- 批量随机抽阵容
- 结果落单个 JSON 文件
- 主结果只保留最终英雄场均汇总
- 按职业分组汇总
- 支持 seed 复现

但当前仍建议注意：
- `exportFullLogs=true` 会明显增加输出量
- 批量非常大时，主要瓶颈还是 Unity batchmode 启动和整体模拟时间
- 如果后面你还想做“双击直接出报表”的更傻瓜化入口，可以再在这层外面包一层更短的 `.bat`

## 关联文件

- [stage-01-offline-simulation-plan.md](/g:/BaiduNetdiskDownload/fight/docs/planning/stage-01-offline-simulation-plan.md:1)
- [run_stage01_offline_sim.bat](/g:/BaiduNetdiskDownload/fight/tools/run_stage01_offline_sim.bat:1)
- [Stage01OfflineSimulationBatch.cs](/g:/BaiduNetdiskDownload/fight/game/Assets/Scripts/Editor/Stage01OfflineSimulationBatch.cs:1)
