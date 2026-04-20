# 战斗侧边栏单英雄面板接入说明

最后更新：2026-04-20

## 文档用途

这份文档用于给后续 AI 或实现侧边栏运行时 HUD 的开发者快速交接：

- 当前静态预览由哪个脚本生成
- 脚本里每个绘制区块分别代表什么游戏信息
- 哪些部分只是预览辅助，不应直接当作运行时代码
- 后续接入 Unity 时，建议准备哪些字段

当前参考脚本：
- `tools/mockups/generate_side_hero_sidebar_mockup.py`

当前参考输出：
- `game/Assets/Art/UI/Mockups/side_hero_sidebar_mockup_v8.png`
- `game/Assets/Art/UI/Mockups/side_hero_sidebar_mockup_v8_preview.png`

注意：
- 这个 `py` 文件是静态样机生成器，不是运行时 UI 逻辑。
- 运行时接入时，应把它视为“布局和视觉标注说明”，而不是直接嵌进 Unity 主逻辑。

## 脚本结构总览

脚本可以粗分成 3 类内容：

1. 尺寸与素材基础
- `s()`，第 168 行：把设计单位转换成最终像素。
- `rect()`，第 172 行：用设计单位快速生成矩形区域。
- `load_icon()`，第 310 行：加载并染色图标，保留比例。
- `load_rotated_icon()`，第 316 行：加载图标后旋转，用于竖向剑图标。

2. 组件绘制辅助
- `draw_icon_value_group()`，第 326 行：用于底部“图标 + 数值”居中组合。
- `compose_avatar()`，第 359 行：生成当前头像占位图，只是预览头像，不是正式角色立绘流程。
- `draw_arrow_button()`，第 395 行：生成右上角状态接口按钮。

3. 整体布局生成
- `make_preview()`，第 412 行：给最终卡片加外部暗背景，仅用于看预览图。
- `generate()`，第 432 行：真正定义侧边栏所有信息区域。
- `write_meta()`，第 536 行：给导出的 png 写 Unity `.meta`，只服务资源落库。

## 运行时信息映射

下面这张表是后续接入最重要的部分。

| 脚本区块 | 代码位置 | 当前区域含义 | 运行时应绑定的数据 |
| --- | --- | --- | --- |
| 顶部左页签 | `# Header tabs`，第 450 行附近 | 左上深色“资讯”标签 | 可先固定文案；如果后面有切页需求，可改成页签状态 |
| 顶部红条主标题 | 第 456 行 | 当前选手名字 | `PlayerName` |
| 右上角箭头按钮 | 第 457 行、`draw_arrow_button()` 第 395 行 | 预留的选手状态接口 | `PlayerStateIcon` / `PlayerStateType` / `StatusBadge`，当前可只保留空接口 |
| 左侧 KDA 表头与数值 | `# Left stat column`，第 463 行附近 | 当前英雄的 K/D/A | `Kills`、`Deaths`、`Assists` |
| 左侧剑图标行 | 第 477-485 行附近 | 当前英雄造成的伤害 | `DamageDealt` |
| 左侧盾图标行 | 第 477-485 行附近 | 当前英雄承受的伤害 | `DamageTaken` |
| 左侧生命图标行 | 第 477-485 行附近 | 当前英雄造成的回复量 | `HealingAndShieldDone` |
| 中部头像框 | `# Portrait block`，第 488 行附近 | 英雄头像占位图 | `HeroPortraitSprite` |
| 右侧三条特性栏 | `# Trait reserve`，第 495 行附近 | 选手 3 条特性预留位 | `Trait1`、`Trait2`、`Trait3` |
| 底部攻防面板 | `# Core stats section`，第 506 行附近 | 当前攻击力 / 防御力 | `CurrentAttack`、`CurrentDefense` |

## 每个区块的更细说明

### 1. 顶部红条

对应代码：
- `centered_text(draw, rect(28, 0, 93, 23), "选手名", ...)`

含义：
- 这里现在不是系统标题，而是选手名字显示区。
- 后续如果是左右两边各 5 个面板，建议这里直接显示选手名或选手简称。

运行时建议：
- 文本过长时要做截断或缩放。
- 如果后续需要显示职业或英雄名，建议另外加小字，不要挤占这里的主名字位。

### 2. 右上角状态接口

对应代码：
- `draw_arrow_button(card, rect(121, 2.2, 18, 18))`
- `draw_arrow_button()` 第 395 行

含义：
- 这是预留接口，不是确定的功能文案。
- 现在只表达“这里未来可以挂一个选手状态/状态按钮/状态角标”。

运行时建议：
- 当前没有对应状态系统时，可以保留静态样式。
- 真接入时，可替换成：
  - 连胜/连败状态
  - 临时 buff/debuff 提示
  - 手感/士气/发挥状态
  - 网络/断线类状态角标

### 3. 左侧数据列

对应代码：
- `# Left stat column`
- KDA 文本：第 470-474 行附近
- 三个图标统计：第 477-485 行附近

含义：
- 这一列是“当前这个英雄”的实时战斗统计，不是全队统计。
- 三条图标信息固定为：
  - 剑：造成伤害
  - 盾：承受伤害
  - 生命：造成回复量，包含治疗和护盾

运行时建议：
- 数据刷新频率可以比血条更低，例如 0.2 秒到 0.5 秒刷新一次。
- 这些值建议统一来自战斗结算/事件累计层，不要由 UI 自己计算。

### 4. 头像框

对应代码：
- `portrait_slot = rect(33, 27, 33, 33)`
- `compose_avatar()` 仅用于生成预览占位图

含义：
- 当前脚本里的人脸是临时占位头像，不代表正式角色头像生成方式。

运行时建议：
- Unity 接入时，应直接使用英雄头像 Sprite。
- 不要沿用 `compose_avatar()` 的逻辑作为正式运行时头像方案。

### 5. 特性区

对应代码：
- `trait_rows = [...]`

含义：
- 这里固定预留 3 条选手特性。
- 当前文案 `特性 1/2/3` 只是占位。

运行时建议：
- 最简单接入方式是 3 个字符串。
- 如果后续特性有颜色、图标、层级，建议每条做成独立 View 数据：
  - `Label`
  - `Icon`
  - `StateColor`
  - `IsActive`

### 6. 攻防主属性区

对应代码：
- `bottom_box = rect(29, 62, 110, 26)`
- `draw_icon_value_group(...)`

含义：
- 当前只放两个核心即时属性：
  - 剑：当前攻击力
  - 盾：当前防御力

运行时建议：
- 这里应显示“当前值”，不是基础值。
- 也就是它应吃到 buff / debuff / 装备 / 临时效果后的结果。

## 目前不应直接搬进运行时的部分

下面这些代码主要是为了快速出图，不建议直接当作运行时结构复用：

- `compose_avatar()`：只是头像占位生成器。
- `make_preview()`：只是给预览图加外部背景。
- `write_meta()`：只是生成 Unity 资源导入的 `.meta` 文件。
- `parse_args()` / `main()`：只是命令行导出入口。

## 建议的运行时数据结构

后续 AI 如果要把这个侧边栏接进 Unity，可以先准备一个纯 UI 读取对象，避免 UI 直接碰战斗逻辑。

示例字段建议：

```csharp
public sealed class HeroSidebarViewData
{
    public string PlayerName;
    public Sprite HeroPortrait;

    public int Kills;
    public int Deaths;
    public int Assists;

    public int DamageDealt;
    public int DamageTaken;
    public int HealingAndShieldDone;

    public int CurrentAttack;
    public int CurrentDefense;

    public string Trait1;
    public string Trait2;
    public string Trait3;

    public Sprite PlayerStateIcon;
    public bool ShowPlayerState;
}
```

如果后续要支持左右两边镜像布局，建议：
- 保持一份相同的数据结构
- 在表现层做左右镜像，不要复制两套业务字段

## 推荐接入顺序

1. 先把这张图拆成“底板 + 文本位 + 图标位”的运行时结构。
2. 先接静态假数据，确认位置和裁切正确。
3. 再把 KDA、伤害、承伤、治疗接到战斗结果累计层。
4. 再把攻防值接到运行时属性读取层。
5. 最后再接右上角状态接口和 3 条特性。

## 给后续 AI 的一句话提示

如果你要把 `generate_side_hero_sidebar_mockup.py` 接进游戏：
- 把 `generate()` 看成布局说明书
- 把 `compose_avatar()` / `make_preview()` / `write_meta()` 看成预览辅助
- 真正需要绑定的数据位，主要就是：名字、头像、KDA、伤害/承伤/治疗、3 条特性、攻防、右上状态接口
