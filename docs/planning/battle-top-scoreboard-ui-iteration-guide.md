# 战斗顶部计分板改版交接文档

最后更新：2026-04-20

## 这份文档是给谁看的

这份文档是写给“接手继续改战斗顶部计分板的另一个 AI”看的。

目标不是讲抽象原则，而是把下面几件事讲清楚：

1. 这张图最开始是怎么做出来的。
2. 为什么中途先不急着接入游戏，而是先做静态素材。
3. 为了方便快速改图，额外写了什么脚本。
4. 用户每一轮反馈后，具体是怎么对应到版式修改的。
5. 最后又是怎么把静态图拆成“运行时底板 + 动态 HUD”的。

---

## 先说结论

这张顶部计分板**不是在 Unity 里直接边写边调出来的**，也**不是一次性手工画死的**。

实际工作流分成两段：

1. **静态素材阶段**
   先用 Python 脚本快速生成多版 PNG，让用户先把视觉方向敲定。
2. **运行时接入阶段**
   等布局基本稳定后，再把静态图里的“不会变的部分”做成底板资源，把“会变的部分”交给 `BattleHud` 动态绘制。

这样做的原因很实际：

- 用户一开始明确说了：`先把计分板做出来，做成素材我看看效果，先不急着接入游戏`
- 如果一开始就直接进 Unity 边写边调：
  - 改一处要进场景看一次
  - 布局很难快速来回试
  - 用户每次指出一个细节，都要和运行时逻辑绑在一起改
- 先做静态图可以把节奏拆开：
  - 先解决“风格和布局”
  - 再解决“真实数据怎么挂进去”

---

## 这次真正用到的关键文件

### 1. 静态素材生成脚本

`tools/mockups/generate_top_scoreboard_mockup.py`

这是这次最重要的“快调图”工具。

它的职责是：

- 用 Pillow 组合底板、缎带、中间装饰、圆点、文字
- 输出版本化的静态图：
  - `top_scoreboard_mockup_v1.png`
  - `top_scoreboard_mockup_v2.png`
  - ...
  - `top_scoreboard_mockup_v8.png`
- 同时输出预览图
- 自动补 `.meta`，方便 Unity 识别导入

### 2. 静态产物目录

`game/Assets/Art/UI/Mockups/`

这里放用户确认视觉方向时看的版本图。

### 3. 最终运行时底板

`game/Assets/Resources/UI/BattleHud/top_scoreboard_runtime_base.png`

这是把最终确认版 HUD 中**不需要动态变化**的部分单独裁出来的运行时资源。

### 4. 最终接入脚本

`game/Assets/Scripts/UI/BattleHud.cs`

这里不是简单贴一张整图，而是：

- 画运行时底板
- 再叠加动态比分
- 动态时间
- 动态加时文本
- 占位队名
- 占位小局圆点

---

## 为什么一定要先写生成脚本

### 背景

用户给的是“连续细改”的需求，不是一次性大方向：

- 先做顶部计分板方向
- 再删中间某个字
- 再对齐队徽位
- 再修左右圆点不对称
- 再删黑条
- 再把队名下移
- 再把圆点挪到数字下方
- 再把圆点往下沉一点
- 再把多余装饰删掉

这种需求如果每次都手动画图，或者每次都去 Unity 里直接硬摆，成本非常高。

### 所以脚本化的目的是什么

`generate_top_scoreboard_mockup.py` 不是为了“炫技”，而是为了**把所有版式调整变成改几行数字就能重出图**。

它解决了三个问题：

1. **坐标集中管理**
   例如缎带位置、计分区距离中轴、圆点位置、队名框高度，都在脚本里直接改。
2. **反复导出很快**
   用户一句“这个位置左右还是不对称”，可以马上改几组坐标重出一版，不需要重新搭 UI。
3. **版本可追溯**
   每一轮都导出一个明确版本号，用户可以直接比较：
   - `v3` 和 `v4` 哪个对
   - `v6` 和 `v7` 差在哪里

### 另一个 AI 接手时必须知道

如果还要继续改这套顶部计分板的“造型”，**优先改这个脚本**，不要先去手工修改最终 PNG，也不要先去 Unity 里一点点瞎挪。

静态图阶段优先入口就是：

`python tools/mockups/generate_top_scoreboard_mockup.py`

---

## 静态图阶段到底是怎么做的

### 用到的底层素材来源

静态图不是完全从零硬画的，复用了仓库里已有 UI 素材：

来源目录：

`game/Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component`

主要用到：

- `Label-Title/Title_Ribbon_03_Blue.png`
- `Label-Title/Title_Ribbon_03_Red.png`
- `Frame/LineTextFrame_04_Demo.png`
- `Frame/LineFrame_02.png`

然后再用脚本补上：

- 斜切外框底板
- 队徽槽位
- 中间发光装饰
- 数字
- 小圆点
- 文本阴影

### 核心做法

脚本里有几个关键函数：

- `slanted_panel()`
  负责画顶部总底板轮廓。
- `make_logo()`
  负责画队徽位里的圆形占位徽记。
- `centered_text()`
  负责按给定 box 居中文字。
- `generate()`
  负责把整张图拼起来并输出。
- `write_meta()`
  负责给新导出的 PNG 自动补 Unity `.meta`。

### 重要认知

这套方法的核心不是“绘图技术”，而是：

> 把 UI 改版问题，转换成“少量几何参数反复迭代”的问题。

---

## 用户要求是怎么一步步落成图的

下面按实际迭代顺序写，不是按代码模块写。

### 阶段 1：先定方向，不急着接入游戏

用户要求：

- 顶部计分板需要重做
- 需要当前人头、剩余时间、小局比分
- 两边要留空间放队伍名字和图片
- 先做素材看看效果，不急着接入游戏

对应处理：

- 不直接改 `BattleHud`
- 先创建 `tools/mockups/generate_top_scoreboard_mockup.py`
- 先导出静态图给用户确认方向

相关提交：

- `37a97b4 ui: add top scoreboard mockup preview assets`
- `b169d70 ui: refine top scoreboard mockup alignment`

### 阶段 2：删中间“小局 2:1”文字，统一对齐

用户要求：

- 中间“小局 2:1”的文字删掉
- 队徽位和旁边黑框高度位置不一样，要对齐

对应处理：

- 删除中间小局说明文字，只保留后续要用的小圆点表达
- 调整队徽槽、黑框、中心区的整体高度关系

这里的经验是：

- 用户说“方向对了，继续往下改”，说明风格可以保留
- 这时不要换整套素材，只改几何关系

### 阶段 3：处理“队徽位像有两个”和蓝方数字压圆点

用户指出：

- 队徽位出现了两个
- 蓝方击杀数把一个圆点挡住了

对应处理：

- 删掉“队徽位”占位文字，不再让人误以为有双层占位
- 把队徽内部改成更收敛的圆形标记，而不是里外两层方框
- 把蓝方数字和圆点拉开

相关提交：

- `a582f78 ui: fix scoreboard mockup spacing`

### 阶段 4：发现“不是圆点自己不对称，而是整组离中轴不对称”

用户指出：

- 左蓝点和右红点位置不对称
- 和中间 `VS` 的距离明显不一致

第一次处理还不够，因为只把圆点本身做了镜像，但**数字和圆点整组相对中轴还是不对称**。

最后采用的正确做法是：

- 不单独挪圆点
- 先定义“左右分数盒”
- 再根据分数盒推导圆点组位置
- 让左右整个“数字 + 圆点”块都由同一组镜像规则生成

相关提交：

- `e3e3041 ui: align scoreboard round dots symmetrically`
- `d01dfb4 ui: mirror scoreboard score layout around center`

这是另一个 AI 很容易犯错的地方：

> 只做“点的镜像”是不够的，必须做“整个比分区块”的镜像。

### 阶段 5：用户决定删黑条，把队名移下来

用户明确要求：

- 黑条删掉
- 把上面的队伍名字部分移到这里
- 小圆点挪到数字下面
- 两边对称
- 队伍名字在缎带图案中间

对应处理：

- 删除左右大黑条承载区
- 把蓝红缎带下移到左右信息区内部
- 小圆点改成在数字正下方的布局
- 队名文字不再按外部大框估算，而是按**缎带自身 box** 居中

相关提交：

- `b293742 ui: restyle scoreboard side name ribbons`

这里的关键经验：

- 用户说“把名字移到这里”，不是说把原有缎带整个原样照搬，而是要把缎带从“上方标题”变成“左右主体的一部分”
- 所以要改的是**缎带角色定位**

### 阶段 6：圆点压数字，继续往下沉

用户指出：

- 圆点压住数字了，往下一点

对应处理：

- 不动左右关系
- 不动点数逻辑
- 只改 `dot_y`

相关提交：

- `da647af ui: lower scoreboard round dots beneath scores`

这类反馈要特别克制：

- 用户没要求重新设计
- 只是要求“纵向再沉一点”
- 所以只做一刀坐标调整，不要顺手改别的

### 阶段 7：删掉“跳舞的波纹装饰”

用户指出：

- 左右侧那种重复描边的“跳舞波纹”不需要
- 多余背景也删掉，想看更接近最终放进游戏里的样子

对应处理：

- 删掉 `slanted_panel()` 里那组循环描边装饰
- 把预览图改成纯 HUD 裁切，不再带额外背景和说明字

相关提交：

- `66e4fad ui: clean scoreboard mockup side decorations`

这一步之后，静态图方向基本稳定，才进入运行时接入阶段。

---

## 最终静态版的关键布局参数

这部分是给另一个 AI 快速定位的。

在 `tools/mockups/generate_top_scoreboard_mockup.py` 里，最终静态版 `v8` 的关键参数大致是：

- 缎带位置：
  - `left_ribbon_x = 166`
  - `right_ribbon_x = 1194`
  - `ribbon_y = 118`
  - `ribbon_width = 560`
  - `ribbon_height = 84`
- 中轴计分区：
  - `center_axis_x = 960`
  - `score_gap_from_axis = 102`
  - `score_box_width = 84`
- 圆点：
  - `dot_size = 18`
  - `dot_spacing = 28`
  - `dot_y = 184`

如果后面还要继续调“静态造型”，从这些数字下手最有效。

---

## 从静态图接进游戏时，是怎么想的

### 不是直接把整张 PNG 糊进 HUD

用户确认完静态方向后，下一步是接进战斗界面。

但这里不能直接把 `top_scoreboard_mockup_v8.png` 整张当 HUD，因为：

- 比分是实时变化的
- 时间是实时变化的
- 加时状态是实时变化的
- 后续队伍名、队徽、小局比分也应该是动态数据，不应该烤死在图里

所以最后采用的是：

1. **把不会变的部分做成底板**
2. **把会变的部分运行时画上去**

### 运行时底板是什么

实际生成并接入的资源是：

`game/Assets/Resources/UI/BattleHud/top_scoreboard_runtime_base.png`

它只保留：

- 总外框
- 左右缎带
- 中间时间区装饰
- 左右队徽槽位
- 中间金线等静态装饰

它不包含：

- 实际比分数字
- 实际时间文本
- 实际队名文本
- 实际小圆点状态

### 运行时接入到哪里

接到：

`game/Assets/Scripts/UI/BattleHud.cs`

### 运行时 HUD 现在怎么画

`BattleHud.cs` 的做法是：

1. `Resources.Load<Texture2D>(RuntimeBaseTexturePath)` 读取底板
2. `OnGUI()` 里先画底板
3. 再根据 `BattleContext` 动态叠加：
   - 队名
   - 当前击杀数
   - 常规时间 / 加时赛
   - 计时
   - 小圆点
   - 结束横幅

### 当前运行时里哪些是真数据

已经是真数据的：

- `context.ScoreSystem.BlueKills`
- `context.ScoreSystem.RedKills`
- `context.Clock`
- `context.Clock.IsOvertime`

### 当前运行时里哪些还是占位

还不是正式数据的：

- `blueTeamLabel = "蓝方"`
- `redTeamLabel = "红方"`
- 队徽位里显示的是 `蓝` / `红`
- `blueRoundWins`
- `redRoundWins`

原因不是忘了做，而是**当前数据层本来就没有正式的队伍名、队徽、小局比分字段**。

所以这里必须记住一个边界：

> 不要为了 UI 方便，把假数据硬塞进战斗核心。

这次是先用安全占位挂进去，保持 `BattleHud` 仍然只是读取者。

---

## 运行时接入后，用户又继续提了哪些微调

### 1. 删掉“存活 x/x”

用户要求：

- 把 `存活 x/x` 删掉

对应处理：

- 从 `BattleHud.cs` 顶部计分板绘制逻辑里移除两行存活数
- 把对应的无用样式和方法一起删掉

相关提交：

- `b4cdc61 ui: remove alive counters from battle hud`

### 2. 队名在缎带高度上不居中

用户指出：

- 因为之前底下还有字，现在删了以后，队名高度看起来不在缎带中间

对应处理：

- 把队名文本框从“上半区高度”恢复为“整条缎带高度”
- 即把文本框高度从旧的 `44` 扩成 `76`

相关提交：

- `c7a9f3a ui: center battle hud team names on ribbons`

这也是一个很典型的“联动问题”：

- 原先队名并不是真的偏了
- 是因为之前下面还有第二行信息，队名 box 被设计成上半部分居中
- 当第二行删掉后，就必须同步重设队名 box

---

## 这次相关提交，另一个 AI 可以按这个顺序看

如果另一个 AI 想快速复盘，不要从整个大仓库乱翻，按这个顺序看最有效：

1. `37a97b4 ui: add top scoreboard mockup preview assets`
2. `b169d70 ui: refine top scoreboard mockup alignment`
3. `a582f78 ui: fix scoreboard mockup spacing`
4. `e3e3041 ui: align scoreboard round dots symmetrically`
5. `d01dfb4 ui: mirror scoreboard score layout around center`
6. `b293742 ui: restyle scoreboard side name ribbons`
7. `da647af ui: lower scoreboard round dots beneath scores`
8. `66e4fad ui: clean scoreboard mockup side decorations`
9. `418f32b ui: integrate top scoreboard into battle hud`
10. `b4cdc61 ui: remove alive counters from battle hud`
11. `c7a9f3a ui: center battle hud team names on ribbons`

---

## 另一个 AI 接手时应该怎么继续做

### 如果要继续调“造型”

优先改：

`tools/mockups/generate_top_scoreboard_mockup.py`

流程：

1. 调坐标 / 字体框 / 间距
2. 重新导出静态图
3. 先给用户看图
4. 再决定是否同步改运行时底板和 `BattleHud`

### 如果要继续调“真实运行时显示”

优先改：

`game/Assets/Scripts/UI/BattleHud.cs`

适合这里改的内容：

- 队名文字框位置
- 时间文本格式
- 击杀数字位置
- 圆点颜色和大小
- 运行时缩放规则

### 如果要把队伍名 / 队徽 / 小局比分做成真数据

不要直接写死在 `BattleHud.cs`。

更合理的方向是：

- 给 `BattleInputConfig` 或相关队伍数据结构补：
  - `displayName`
  - `badgeSprite`
  - `matchRoundWins` 或等价字段
- 再让 `BattleHud` 从输入对象或流程状态里读取

---

## 另一个 AI 最容易踩的坑

### 坑 1：一上来就直接改 Unity 运行时 HUD，不先做静态验证

这次用户的工作方式明显是“盯图一点点收细节”。

如果直接从 Unity 场景里硬调，很慢，也很难快速对比版本。

### 坑 2：只镜像圆点，不镜像整个比分块

用户已经明确指出过一次：

- 看起来不对称，不是因为点没对称
- 而是因为“点 + 数字”整组离中轴不一致

### 坑 3：删了下方文字，不同步重设队名 box

这次队名偏上的根因就是这个。

### 坑 4：把整张静态图直接糊进 HUD

这样时间和比分都会死掉，后面也很难接真实数据。

### 坑 5：为了 UI 方便，把临时队名或小局比分写进战斗逻辑

UI 应该读数据，不应该反向接管战斗事实。

---

## 最后一句给接手 AI 的操作建议

如果你要继续改这套顶部计分板，正确顺序是：

1. 先判断这次改的是“造型”还是“运行时数据绑定”
2. 改造型先改 `tools/mockups/generate_top_scoreboard_mockup.py`
3. 改运行时表现再改 `game/Assets/Scripts/UI/BattleHud.cs`
4. 不要跳过静态图确认，直接去 Unity 里乱调
5. 不要把队名 / 小局比分 / 队徽假数据写进战斗核心

简单说：

> 这次顶部计分板能顺利落下来，靠的不是一次做对，而是“先脚本化出静态图，再按用户反馈一轮轮收形，最后再做动态接入”。

