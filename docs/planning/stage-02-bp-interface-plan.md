# 第二阶段真实 BP 界面规划

最后更新：2026-04-28

## 文档用途

这份文档记录第二阶段 `真实 BP 界面` 的目标、边界、第一版规则和落地顺序。

第一阶段的 `HeroSelect` 只负责把双方阵容填满并进入战斗；它不是最终 BP。第二阶段开始后，后续 AI 不应再把 BP 当作第一阶段的范围膨胀，而应按本文约束推进。

## 第二阶段目标

第二阶段当前目标是做出本地单机可用的 Ban/Pick 界面，并把结果继续接入现有自动战斗通路。

完成标准：
- 可以从英雄池中按 BP 阶段禁用英雄
- 可以按 BP 阶段为蓝红双方选择英雄
- 被禁用英雄不可选择
- 已选择英雄不可被其他槽位重复选择
- 双方各完成 `5` 名英雄后，可以生成战斗输入并进入 Battle
- BP 界面能显示英雄头像、职业、标签、基础属性、小技能和大招摘要
- 普攻不单独占用摘要槽；射程等基础普攻信息并入属性行，少数带额外机制的普攻后续可用轻量备注处理

## 当前不做

第二阶段第一版仍不做：
- 赛季系统
- 经营系统
- 联机 BP
- 房间 / 匹配 / 账号系统
- 自动 AI 选人教练
- 复杂赛事规则
- 完整存档系统

说明：
- BP 可以成为后续赛季、经营和联机的入口，但第一版只做本地单机 BP 到战斗输入。

## 参考界面结构

第二阶段 BP 界面参考《Teamfight Manager》式阅读结构，而不是继续沿用第一阶段三栏 IMGUI 占位样式。

界面应包含：
- 顶部对战栏：蓝方名称、红方名称、比分或占位信息、当前阶段
- 阶段提示栏：例如 `蓝方禁用 1/2`、`红方选择 3/5`
- 左侧蓝方阵容栏：5 个队员 / 英雄槽位
- 右侧红方阵容栏：5 个队员 / 英雄槽位
- 中央英雄池：头像网格、职业颜色、禁用 / 已选遮罩
- 英雄详情面板：选中英雄的职业、标签、属性、小技能、大招说明
- 职业筛选栏：全部、战士、法师、刺客、坦克、辅助、射手
- 底部禁用区：双方已禁用英雄
- 确认按钮：确认当前阶段操作

## 第一版 BP 规则

第一版 BP 规则先做成可配置顺序，但默认规则如下：

- 队伍规模：蓝红双方各 `5` 名英雄
- 英雄池：读取 `Stage01HeroCatalog`
- 禁用数量：蓝红双方各 `3` 个 Ban 位
- 选择数量：蓝红双方各 `5` 个 Pick 位
- 默认流程：
  1. 蓝方 Ban 1
  2. 红方 Ban 1
  3. 蓝方 Ban 2
  4. 红方 Ban 2
  5. 蓝方 Pick 1
  6. 红方 Pick 1
  7. 红方 Pick 2
  8. 蓝方 Pick 2
  9. 蓝方 Pick 3
  10. 红方 Pick 3
  11. 红方 Ban 3
  12. 蓝方 Ban 3
  13. 红方 Pick 4
  14. 蓝方 Pick 4
  15. 蓝方 Pick 5
  16. 红方 Pick 5

合法性规则：
- 已禁用英雄不能再被禁用或选择
- 已选择英雄不能再被禁用或选择
- 同一局内默认不允许蓝红双方重复选择同一英雄
- 当前阶段只能由当前操作方执行当前动作
- 未完成全部 Pick 前不能进入战斗

后续如果要改成蛇形选择、更多 Ban 位、隐藏选择或教练自动 BP，应先更新本文，再改实现。

## 数据与职责边界

BP 需要新增或整理的会话状态包括：
- 当前 BP 阶段
- 当前操作方
- 当前高亮英雄
- 已禁用英雄列表
- 蓝方已选英雄列表
- 红方已选英雄列表
- 每个槽位是否锁定
- 当前职业筛选

这些状态属于 BP 会话，不属于英雄静态配置。

禁止做法：
- 不要把禁用、已选、当前高亮状态写回 `HeroDefinition`
- 不要让 UI 直接创建或修改战斗中的 `RuntimeHero`
- 不要让 BP UI 承担战斗胜负、技能、伤害、复活或计分规则

允许做法：
- BP UI 读取 `HeroDefinition`、`SkillData`、`HeroStatsData` 和头像资源
- BP 完成后组装 `BattleInputConfig`
- `GameFlowState` 或后续替代对象保存待进入 Battle 的运行时输入

## 与现有系统的关系

现有第一阶段入口：
- `game/Assets/Scripts/UI/Flow/HeroSelectSceneController.cs`
- `game/Assets/Scripts/UI/Flow/GameFlowState.cs`
- `game/Assets/Resources/Stage01Demo/Stage01HeroCatalog.asset`
- `game/Assets/Resources/Stage01Demo/Stage01DemoBattleInput.asset`

第二阶段可以复用：
- `Stage01HeroCatalog` 作为英雄池
- `HeroDefinition.visualConfig.portrait` 作为英雄头像
- `HeroDefinition.heroClass` 和 `tags` 作为筛选与卡片信息
- `HeroStatsData` 作为详情面板属性来源，`BasicAttackData` 仅补充射程等基础普攻参数
- `activeSkill` / `ultimateSkill` 作为详情面板技能来源
- `GameFlowState.TryPrepareBattleInput` 的“把已选阵容转为战斗输入”职责

第二阶段需要替换或重构：
- 当前 `HeroSelectSceneController` 的临时三栏 IMGUI 布局
- 当前只判断 `10` 个槽位是否填满的简单选人逻辑
- 当前允许随意覆盖槽位、重复选择的交互方式

## 建议实现顺序

1. 新增 BP 规则和会话状态模型，先不依赖具体 UI 画法。
2. 将 `GameFlowState` 的简单选人状态扩展或替换为 BP 结果状态。
3. 在 `HeroSelect` 场景中先落地可运行 BP 界面，保证能完整走完 Ban/Pick。
4. 接入英雄详情面板，读取头像、职业、标签、属性和技能摘要。
5. 接入职业筛选、禁用 / 已选遮罩和阶段提示。
6. 最后再做视觉细节、动画、素材包皮肤和队伍栏装饰。

## 最低验证

每轮修改后至少验证：
- `HeroSelect` 能打开
- 能按默认流程完成 6 次 Ban 和 10 次 Pick
- 被 Ban 的英雄无法 Pick
- 已 Pick 的英雄无法重复 Pick
- 未完成 BP 时不能 Start Battle
- 完成 BP 后进入 Battle 的蓝红阵容与 BP 结果一致
- BP UI 没有直接改写战斗核心运行时状态

## 对后续 AI 的要求

- 不要再把 Stage 2 BP 误判为第一阶段范围膨胀。
- 也不要借 BP 之名扩到赛季、经营、联机房间或完整赛事系统。
- 如果用户继续调整参考图里的布局、流程或视觉细节，应优先更新本文或补充同目录专项文档。
- 如果实现中发现现有 `HeroDefinition`、`SkillData` 或 `GameFlowState` 字段不足，应先说明缺口，再做最小数据扩展。
