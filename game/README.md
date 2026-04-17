# game

这个目录预留给真正的 Unity 游戏工程。

当前它的用途是：
- 作为未来 Unity 项目的根目录
- 放置真正参与第一阶段编码时需要遵循的 `AGENTS.md`
- 与根目录的规划文档形成分工

当前 Unity 工程已按 `Unity 6000.3.13f1` 初始化。

当前已加入的首批包依赖：
- `Input System`
- `UI Toolkit`
- `Test Framework`
- `Addressables`
- `Localization`

当前第一阶段主通路已经接到场景流程：
- `MainMenu -> HeroSelect -> Battle -> Result`
- `BattleBasicAttackOnly` 仅保留给开发验证

当前 Windows 导出默认输出位置：
- `game/Builds/Windows/FightStage01.exe`

当前批量调数工具入口：
- Unity 菜单 `Fight/Tools/Balance Sheets`
- 默认导出目录：`game/BalanceSheets/Stage01/`

当前调数安全规则：
- 构建前的 demo 内容确保流程只会补齐缺失资产，不会覆盖你已经调好的英雄/技能数值
- 如果确实要把样例内容重置回默认值，只能显式执行 `Fight/Dev/Regenerate Demo Content From Defaults (Overwrite Existing Tuning)`

分工约定：
- 根目录 `AGENTS.md`：总控、规划、阶段约束、文档沉淀
- `game/AGENTS.md`：第一阶段编码规则、工程结构、代码优先级、实现边界

真正开始搭 Unity 工程后，建议把 `Assets/`、`Packages/`、`ProjectSettings/` 等都放在这个目录下。
