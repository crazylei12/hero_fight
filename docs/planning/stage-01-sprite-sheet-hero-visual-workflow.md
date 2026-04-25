# Stage 01 Sprite Sheet Hero Visual Workflow

最后更新：2026-04-25

## 文档用途

这份文档记录“把用户提供的像素动作图接成某个英雄战斗模型”的可复用流程。

适用场景：
- 用户给一张像素图，图中每一行或每一区块是一组动作。
- 用户希望先在 Unity 里看到角色战斗状态，而不是先做完整美术管线。
- 需要把新图临时或正式接到某个已有英雄上。
- 原来的英雄模型需要保留备用，不要直接覆盖或删除。

本流程以 2026-04-25 的风语司铃像素模型接入为例。

## 本次有效结论

这次最有复用价值的地方不是“切了哪几张图”，而是下面几条规则：

- 先做一个动作行预览，让用户能确认每一行动作含义。
- 用户确认动作映射后，再把预览资源转成某个英雄专属的 `HeroPreview` 资源目录。
- 新模型使用新 prefab 文件名，不覆盖旧 prefab。
- 英雄 `HeroDefinition.visualConfig.battlePrefab` 指向新 prefab。
- 如果新 prefab 使用 `SpriteSheetBattleVisualConfig`，`animatorController` 必须置空，避免走 HeroEditor4D 动画控制器。
- 后续样例内容重建脚本也要同步 prefab 路径，否则重建 demo 内容时可能切回旧模型。
- 像素图接进战斗前必须检查 pivot。血条/脚底错位时，优先怀疑 sprite pivot 或帧裁切尺寸，而不是先改血条逻辑。

## 风语司铃案例记录

本次用户给的动作含义如下：

| 用户编号 | 原动作行 | 战斗动作 |
| --- | --- | --- |
| 1 | `Action01` | `Idle` |
| 2 | `Action02` | `Run` |
| 3 | `Action04` | `Attack1` |
| 4 | `Action05` | `Skill` |
| 5 + 6 | `Action06` + `Action09` | `Ult` |
| 7 | `Action08` | `Death` |
| 未编号备用 | `Action07` | `Hit` |
| 未使用 | `Action03` | 保留备用 |

落地文件：

- 原动作预览资源：`game/Assets/Resources/HeroPreview/chatgpt_20260425_bellcaster/`
- 风语司铃专属战斗帧：`game/Assets/Resources/HeroPreview/support_002_windchime_bellcaster/`
- 新战斗 prefab：`game/Assets/Prefabs/Heroes/support_002_windchime/WindchimeBellcaster.prefab`
- 旧模型保留：`game/Assets/Prefabs/Heroes/support_002_windchime/Windchime.prefab`
- 英雄数据：`game/Assets/Data/Stage01Demo/Heroes/support_002_windchime/Windchime.asset`
- 自动生成/同步脚本：`game/Assets/Scripts/Editor/WindchimeBellcasterVisualBuilder.cs`
- 样例内容生成器引用：`game/Assets/Scripts/Editor/Stage01SampleContentBuilder.cs`

本次提交：

- `b3bf4dd`：接入风语司铃新像素模型。
- `9e43a56`：把风语司铃新模型 pivot 从中心点改为脚底点，修正血条/脚底错位。

## 推荐落地流程

### 1. 先做预览，不急着替换英雄

用户给图后，先生成一个独立预览：

- 原图放在 `game/Assets/Art/Heroes/<source_key>/source_sheet.png`。
- 拆帧放在 `game/Assets/Resources/HeroPreview/<source_key>/ActionXX/`。
- 预览 prefab 放在 `game/Assets/Prefabs/Heroes/<source_key>/`。
- 如有需要，生成一个预览 scene 方便用户在 Unity 中看动作行。

这个阶段的目标是让用户确认：

- 哪一行是 idle。
- 哪一行是移动。
- 哪一行是普攻。
- 哪一行是小技能。
- 哪几行拼成大招。
- 哪一行是死亡。
- 有没有受击、备用或未使用动作。

### 2. 用户确认后，再生成英雄专属资源

不要直接让英雄使用通用预览目录。

推荐把帧复制或生成到英雄专属目录：

```text
game/Assets/Resources/HeroPreview/<hero_id>_<visual_key>/
  Idle/
  Run/
  Attack1/
  Skill/
  Ult/
  Hit/
  Death/
```

动作 key 优先使用现有 `SpriteSheetBattleAnimationDriver` 识别的名字：

- `Idle`
- `Run`
- `Attack1`
- `Attack2`，没有就不必强行做
- `Skill`
- `Ult`
- `Hit`
- `Death`

如果某个动作没有专用帧，可以先复用最接近的动作，但要在文档或脚本里写清楚。

### 3. 新建 prefab，不覆盖旧模型

当新图用于替换已有英雄时：

- 新 prefab 使用清晰名字，例如 `WindchimeBellcaster.prefab`。
- 旧 prefab 原地保留，例如 `Windchime.prefab`。
- 英雄 asset 只改 `visualConfig.battlePrefab` 指向新 prefab。
- 如果未来想切回旧模型，只需要把 `battlePrefab` 指回旧 prefab。

新 prefab 至少需要：

- 根节点 `GameObject`
- `SortingGroup`
- `SpriteRenderer`
- `SpriteTextureFrameAnimator`，用于编辑器里预览 idle
- `SpriteSheetBattleVisualConfig`，用于战斗运行时加载各动作帧

### 4. 同步样例内容生成器

如果对应英雄在 `Stage01SampleContentBuilder` 里有 prefab 常量或默认生成逻辑，也必须同步。

必须检查：

- `LoadBattlePrefab` 是否会返回旧 prefab。
- 默认生成英雄时是否会把 `animatorController` 重新填回 HeroEditor4D 控制器。
- 对使用 `SpriteSheetBattleVisualConfig` 的 sprite 模型，`animatorController` 应保持 `null`。

这一步很重要，否则后续执行 demo 内容重建时，会把用户刚确认的新模型覆盖回旧配置。

## Pivot 和血条对齐规则

战斗血条不是按具体美术自动贴身计算的。

当前 `BattleView` 中，所有英雄的血条都挂在单位根节点下的 `FootUiRoot`，并使用统一偏移：

```text
footUiOffset = (0, -0.36, 0)
```

因此血条位置异常时，优先排查：

- sprite pivot 是否在图片中心。
- 帧裁切是否包含大量底部透明像素。
- 每帧尺寸是否差异过大。
- prefab 根节点或 visual 子节点是否额外偏移。
- `pixelsPerUnit` 和 prefab scale 是否让角色体型明显异常。

风语司铃这次的问题：

- 新 prefab 最初使用 `{x: 0.5, y: 0.5}` 中心 pivot。
- 角色脚底实际在图片底部约 `8%` 位置。
- 血条逻辑没有偏，错位来自模型原点不在脚底。
- 修正方式是把 `spritePivot` 改为 `{x: 0.5, y: 0.08}`。

后续推荐做法：

- 对站立/idle 第一帧计算不透明像素的底部边界。
- 用 `bottomTransparentPixels / frameHeight` 估算脚底 pivot。
- 常见像素小人若脚底接近底部，pivot y 通常在 `0.05` 到 `0.12` 之间。
- 不要为了单个模型去改全局血条偏移，除非确认所有英雄都需要调整。

## 验证清单

每次接入新像素英雄后，至少检查：

- Unity 能看到新 prefab。
- 旧 prefab 仍然存在。
- 英雄 asset 的 `battlePrefab` 指向新 prefab。
- sprite 模型英雄的 `animatorController` 是 `null`。
- `SpriteSheetBattleVisualConfig.resourcesRoot` 指向英雄专属资源目录。
- `Idle`、`Run`、`Attack1`、`Skill`、`Ult`、`Hit`、`Death` 至少能加载到帧。
- 角色脚底、影子、光环、血条大致对齐。
- 移动、普攻、小技能、大招、死亡时不会出现明显跳位。
- 后续重建 demo 内容不会把配置切回旧 prefab。

如果 Unity 项目已经被编辑器打开，batchmode 可能会因为项目锁失败。这种情况下可以：

- 让当前打开的 Unity 刷新导入资源。
- 或在不关闭用户工作的前提下，直接生成 Unity YAML 资源，再由编辑器刷新识别。
- 最后仍要检查 `Editor.log` 里是否有 C# 编译错误。

## 后续可改进

目前流程仍偏手工，后续可以把它收敛为一个通用 editor 工具：

- 输入源动作目录。
- 输入目标 hero id 和 visual key。
- 在 UI 里配置动作映射。
- 自动复制帧、生成 prefab、更新 hero asset。
- 自动估算脚底 pivot。
- 自动生成一个临时 battle preview 场景或测试阵容。

在通用工具完成前，可以继续参考 `WindchimeBellcasterVisualBuilder.cs` 作为专用 builder 模板。
