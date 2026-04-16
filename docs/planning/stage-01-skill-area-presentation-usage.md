# 第一阶段战斗特效表现使用说明

最后更新：2026-04-16

## 文档用途

这份文档用于说明第一阶段当前“战斗特效表现系统”应该怎么使用，以及后续 AI 在接手技能区域特效、普通攻击飞行物特效、顶视角战斗 VFX 调整时，默认应该按什么流程做。

它主要解决以下问题：

- 技能逻辑范围与视觉范围如何对齐
- 飞行物逻辑轨迹与视觉外观如何对齐
- 为什么资源包自带 prefab 往往不能直接当作最终成品
- 什么时候该直接使用 prefab
- 什么时候该先组装项目专用 prefab
- 什么时候才应该补一层代码

## 当前默认原则

一句话：

- `逻辑判定` 由战斗系统决定
- `视觉表现` 由表现层适配逻辑参数
- `资源包 prefab` 默认只是素材来源
- `项目最终使用的 prefab` 应优先是为本项目重新整理过的版本

后续 AI 在接到“我要一个某某特效”的需求时，默认按以下顺序执行：

1. 先确认这个特效在战斗里的语义
2. 再确认它需要吃哪些逻辑参数
3. 优先使用现有资源包里的粒子、Sprite、材质、子特效，组装出一个“项目专用 prefab”
4. 只有当 prefab 本身无法表达逻辑联动时，才补一层很薄的通用驱动代码
5. 最后由程序去调用这个项目专用 prefab，而不是直接调用资源包原始 prefab

默认规则：

- 先做 prefab，后接程序
- 资源包是原料，不是最终成品
- 少写英雄专属的大段表现代码
- 真需要代码时，也优先写可复用的通用层

## 项目专用 prefab 的存放规则

后续 AI 新建或整理好的项目专用战斗特效 prefab，默认放在：

- `game/Assets/Prefabs/VFX/`

推荐继续按用途拆子目录，例如：

- `game/Assets/Prefabs/VFX/Skills/`
- `game/Assets/Prefabs/VFX/Projectiles/`
- `game/Assets/Prefabs/VFX/Shared/`

规则：

- 不要直接在资源包原目录里改出“项目最终版”
- 资源包目录保留为素材来源
- 项目最终真正引用的，应尽量指向我们自己整理后的 prefab

## 当前 prefab 构建入口

当前已经提供的项目专用 VFX 构建入口：

- 编辑器菜单：`Fight -> Stage 01 -> Build FireMage VFX Prefabs`
- batchmode 执行方法：`Fight.Editor.FireMageVfxPrefabBuilder.BuildFireMageVfxPrefabsBatch`

当前这条 builder 已覆盖：

- 火法普攻投射物
- 火法小技能 `Ember Burst`
- 火法大招 `Meteor Fall`

执行注意事项：

- 如果 Unity 工程已经在另一个编辑器实例中打开，batchmode 不能再打开同一个项目
- 此时应直接在当前打开的 Unity 编辑器里执行菜单项，或者先关闭编辑器再跑 batchmode

## 当前相关代码与资产位置

当前战斗特效表现系统的关键文件：

- `game/Assets/Scripts/Data/SkillData.cs`
- `game/Assets/Scripts/Data/HeroVisualConfig.cs`
- `game/Assets/Scripts/Battle/RuntimeSkillArea.cs`
- `game/Assets/Scripts/Battle/RuntimeBasicAttackProjectile.cs`
- `game/Assets/Scripts/UI/BattleView.cs`
- `game/Assets/Scripts/Editor/FireMageVfxPrefabBuilder.cs`
- `game/Assets/Scripts/UI/Presentation/Skills/SkillAreaPresentationController.cs`
- `game/Assets/Scripts/UI/Presentation/Skills/FireSeaSkillAreaPresentationController.cs`
- `game/Assets/Scripts/Data/SkillAreaPresentationType.cs`
- `game/Assets/Scripts/Editor/Stage01SampleContentBuilder.cs`
- `game/Assets/Art/VFX/Generated/vfx_soft_circle.png`

当前法师大招配置资产：

- `game/Assets/Data/Stage01Demo/Skills/mage_001_firemage/Meteor Fall.asset`
- `game/Assets/Prefabs/VFX/Skills/FireMageMeteorField.prefab`

当前火法小技能配置资产：

- `game/Assets/Data/Stage01Demo/Skills/mage_001_firemage/Ember Burst.asset`
- `game/Assets/Prefabs/VFX/Skills/FireMageEmberBurst.prefab`

当前火法普攻配置资产：

- `game/Assets/Data/Stage01Demo/Heroes/mage_001_firemage/FIREMAGE.asset`
- `game/Assets/Prefabs/VFX/Projectiles/FireMageBasicAttackProjectile.prefab`

当前火法 prefab-first 示例已经覆盖：

- 普攻投射物
- 小技能范围爆发
- 大招持续范围

## 当前资源包速查索引

如果后续 AI 只是想“先快速找最可能用得上的资源”，建议优先按这个顺序看：

1. 顶视角区域 / 火焰 / 护盾 / 状态：
   - `Assets/Lana Studio/Casual RPG VFX/`
   - `Assets/Hun0FX/`
   - `Assets/SrRubfish_VFX_03/`
   - `Assets/Super Pixel Effects Pack 1/`
   - `Assets/Super Pixel Effects Pack 2/`
2. 投射物 / 命中 / 发射：
   - `Assets/SF Studio/`
   - `Assets/Super Pixel Projectiles Pack 3/`
   - `Assets/Super Pixel Projectiles Pack 4/`
   - `Assets/Lana Studio/Casual RPG VFX/Prefabs/Range_attack/`
3. 大招级别爆发 / 地裂 / 黑洞 / 龙卷：
   - `Assets/SpecialSkillsEffectsPack/`
   - `Assets/Game VFX -Explosion & Crack/`
   - `Assets/Piloto Studio/`
   - `Assets/Vefects/`
4. 角色 / 场景 / 背景：
   - `Assets/HeroEditor4D/`
   - `Assets/SPUM/`
   - `Assets/Fantasy-Environment/`

### A. 直接优先查的战斗 VFX 包

- `Assets/Lana Studio/Casual RPG VFX/`
  - 主要目录：`Prefabs/Area_generic` `Burst` `Fire` `Fog` `Orbs` `Range_attack` `Regeneration` `Shields` `Slash` `States` `Top_down_attack`
  - 适合：顶视角地面范围、火焰簇、持续火场、护盾、状态环绕、斩击、简单投射物
  - 备注：当前火法普攻 / 小技能 / 大招都已经实际取过这里的火焰与拖尾资源，是目前最符合本项目视角的现成来源之一

- `Assets/Hun0FX/FX/`
  - 主要目录：`BuffnDebuff_vol1`
  - 适合：Buff、Debuff、怒气、元素附着、角色周身状态特效
  - 示例：`FX_Buff_01_fire` `FX_Buff_01_Ice` `FX_Buff_01_Poison` `FX_Debuff_01`

- `Assets/SrRubfish_VFX_03/Prefabs/`
  - 主要族：`Fire_Burst` `Fire_Projectile` `Fire_ImpactBurst` `Fire_ImpactSimple` `Fire_Firewall` `Fire_FlamethrowerBurst` `Fire_PowerArea`
  - 适合：火系飞行物、命中、地火、短持续包裹、火墙类技能
  - 备注：火元素密度很高，但很多 prefab 偏立体，顶视角使用前通常要先拆成项目专用 prefab

- `Assets/Game VFX - Sword Trails/Prefabs/`
  - 主要内容：各种颜色和风格的刀光尾迹
  - 适合：战士 / 刺客 / 剑圣类近战挥砍表现
  - 示例：`FX_SwordT_Bleed` `FX_SwordT_Lightning` `FX_SwordT_Ghost`

- `Assets/Game VFX -Explosion & Crack/Prefabs/`
  - 主要内容：卡通爆炸、冲击、地裂、裂纹、Mesh 版爆炸
  - 适合：命中爆点、地面裂开、终结技、落地爆炸
  - 示例：`FX_CartoonEXP_*` `FX_Crack_Air` `FX_Carck_Splitting`

- `Assets/SpecialSkillsEffectsPack/AllEffects/`
  - 主要目录：`EffectsSet_1(NotScriptBased)` `EffectsSet_2(ScriptBased)`
  - 适合：黑洞、轨道打击、龙卷、核爆、地裂、超大招
  - 示例：`Effect_01_LightningTornado` `Effect_02_BlackHole` `Effect_03_OrbitalStrike` `Effect_05_Nuke` `Effect_08_GroundSlash`
  - 备注：这个包很大、表现偏夸张，也更偏 3D/演示；默认只当原料，不建议整包直挂到当前顶视角战斗里

- `Assets/SF Studio/SF Projectiles Vol 1/`
  - 主要目录：`Prefabs` `Materials` `Models` `Textures`
  - 适合：通用子弹、魔弹、圆环、命中特效
  - 示例：`Bullet 01` `Cone 01` `Projectile 01` `Ring 01` `VFX Hit 01 (Fire/Ice/Magic/Nature)`

- `Assets/SF Studio/SF Projectiles Vol 2/`
  - 主要目录：`Prefabs` `Materials` `Models` `Textures`
  - 适合：元素箭、发射动作、飞行段、命中段
  - 示例：`VFX Arrow Blue/Fire/Earth Launch` `Proj` `Hit`
  - 备注：特别适合弓手、飞针、细长投射物的 prefab-first 组装

- `Assets/Super Pixel Effects Pack 1/Prefabs/`
  - 主要族：`aura_energy` `energy_shield` `impact_hit` `magic_puff` `electric_zap` `explosion_large/small`
  - 适合：像素风 Aura、护盾、命中火花、电击、短爆炸
  - 备注：像素风格和当前主场景并不完全统一，更适合做局部点缀或二次组装的子特效

- `Assets/Super Pixel Effects Pack 2/Prefabs/`
  - 主要族：`bubble_large/small` `impact_shock` `fire_burst` `ice_burst` `electric_burst` `magic_swirl` `claw_large/small` `slash_large/small`
  - 适合：小技能爆点、元素爆发、法术旋涡、爪击 / 斩击瞬态
  - 备注：当前 `Ember Burst` 就已经从这里拿过 `fire_burst` 与 `explosion` 做子特效

- `Assets/Super Pixel Effects Pack 3/`
  - 主要内容：只有 `PNG` 和 `spritesheet`，没有现成 prefab
  - 主要族：`smoke_bomb` `toon_explosion` `impact_hit` `lightning_aura` `magic_spawn` `toon_impact` `dust_trail`
  - 适合：后续自己做 Sprite 动画或在 builder 里手动组装

- `Assets/Super Pixel Explosion FX Pack 1/Prefabs/`
  - 主要族：`symmetrical_exp` `stylized_exp` `epic_exp`
  - 适合：大爆炸、奥义落点、中心爆点
  - 备注：适合“爆炸主体”，不太适合直接做持续地面圈

- `Assets/Super Pixel Projectiles Pack 3/Prefabs/`
  - 主要族：`magic_missile` `magic_orb` `magic_spike` `light_spark` `death_wave` `acid_large/small` `arrow_large/small` `knife_large`
  - 适合：法术弹、光点、箭、飞刀、酸液、命中点缀
  - 备注：当前火法普攻已经实际取过这里的 `magic_missile` 和 `light_spark`

- `Assets/Super Pixel Projectiles Pack 4/Prefabs/`
  - 主要族：`laser_beam` `energy_blast` `energy_pellet` `scrap_metal` `dark_matter` `plasma_ball` `scifi_bomb` `sawblade_large/small`
  - 适合：更科幻的激光、能量弹、锯刃、暗物质球
  - 备注：风格更偏科幻，不一定适合当前奇幻战斗，但某些亮点或拖尾可以拆来复用

- `Assets/Piloto Studio/Elemental VFX Mega Bundle/Poison And Acid/`
  - 主要内容：酸雨、毒泉、毒盾、毒针、毒液飞溅、毒裂纹
  - 适合：毒系英雄、小范围持续伤害区、DOT、减益效果
  - 示例：`1_AcidRain` `10_Poison_Geyser` `11_PoisonSpit` `13_Poison_Shield` `16_Poison_Crack`

- `Assets/Piloto Studio/Elemental Tornados VFX/Prefabs/`
  - 主要内容：不同风格龙卷的 `BuildUp` `Loop` `Dissipation`
  - 适合：召唤龙卷、蓄力后起风、控制类大招
  - 示例：`Tornado_Circus` `Tornado_Shock_Blue_Loop` `Tornado_Tech_Loop`
  - 备注：大多是竖向体积效果，顶视角使用时要先判断是否需要只取底部、环绕或局部粒子

- `Assets/Vefects/Level Up VFX URP/VFX/`
  - 主要内容：升级 / 仪式 / 圆柱 / 圆盘 / 漩涡类效果
  - 适合：升级、觉醒、稀有掉落、角色升阶
  - 示例：`VFX_Level_Up_Customize_*` `LevelUpText`
  - 备注：这个包明显偏 URP；当前项目批处理日志里已经出现过 shader fallback warning，默认不要把它当第一优先源包

### B. 角色、场景与补充视觉包

- `Assets/HeroEditor4D/`
  - 主要目录：`heroes` `FantasyHeroes` `MilitaryHeroes` `UndeadHeroes` `Common`
  - 适合：角色 prefab、动画控制器、装备和少量投射物
  - 备注：当前 `FIREMAGE` 角色本体就来自这里

- `Assets/SPUM/`
  - 主要目录：`Core` `Resources` `Retro UI Set` `Sample` `Ultimate Resource Bundle`
  - 适合：像素 / 纸片式角色资源、样例 UI、替代角色原型
  - 备注：更偏角色系统，不是当前主要 VFX 来源

- `Assets/Fantasy-Environment/`
  - 主要目录：`Prefabs/Buildings` `Props` `Vegetation` `Rocks` 以及 `Particles`
  - 适合：战场背景、建筑、植被、烟雾、火焰、雾气、光束
  - 示例粒子：`Fire01ParticleSys` `Fog01ParticleSys` `Smoke01ParticleSys` `Raylight01ParticleSys`

- `Assets/VFXDemoEnvironment/`
  - 主要内容：环境辅助资源，当前看到的主要是 `Vegetation`
  - 适合：VFX 演示场景的背景或补景
  - 备注：不是主要技能特效来源

- `Assets/FantasyWorkshop/`
  - 主要目录：`FantasyAxes` `FantasySwords` `FantasyPotions` `FantasyRings` `FantasyRunes` 等
  - 适合：武器、饰品、药水、符文、4D 装备补充
  - 备注：更偏角色装备，不是主要 VFX 包

- `Assets/Piloto Studio/Characters/`
  - 主要目录：`Companions/Creatures_01/Models`
  - 适合：补充小型生物或召唤物模型
  - 备注：当前没有检测到 prefab，更多是模型原料

- `Assets/ExplosiveLLC/`
  - 主要目录：`RPG Character Mecanim Animation Pack` `Demo Elements`
  - 适合：演示道具、地表小物件、角色动画
  - 备注：主要价值在动画和 demo props，不在技能特效

### C. 系统、工具、演示与低优先源包

- `Assets/Emerald AI/`
  - 主要内容：AI 系统、Demo、生物角色
  - 补充价值：带有 `Grenadier Projectile / Shoot / Impact` 这类小型枪械示例特效
  - 备注：主要是 AI 包，不是当前主要资源包来源

- `Assets/Feel/`
  - 主要内容：反馈系统、演示场景、振动与反馈工具
  - 适合：屏幕震动、受击反馈、UI 反馈、镜头反馈
  - 备注：更适合“补反馈层”，不是提供大量技能 prefab 的资源包

- `Assets/AdventureCreator/`
  - 主要内容：2D Demo、UI、逻辑 prefab、导航与交互框架
  - 备注：主要是玩法 / UI / 叙事框架，对当前战斗 VFX 基本不是直接资源来源

- `Assets/AstarPathfindingProject/`
  - 主要内容：寻路、示例场景、Navmesh 示例
  - 备注：系统包，没有直接可复用的战斗美术资源

- `Assets/PlayMaker/`
  - 主要内容：可视化脚本系统
  - 备注：工具包，不是资源美术包

- `Assets/Standard Assets/`
  - 主要内容：基础水体、投影、跨平台输入、工具 prefab
  - 适合：极少量通用环境或投影辅助
  - 备注：对当前战斗 VFX 价值较低

### D. 当前看到的空目录或基本可忽略目录

- `Assets/GabrielAguiarProductions/`
  - 当前没有检测到 prefab、scene 或明显可用目录，像是空根目录或未完整导入

- `Assets/_TerrainAutoUpgrade/`
  - 地形升级辅助目录，没有当前战斗 VFX 价值

- `Assets/vFavorites/`
  - 编辑器辅助目录，没有当前战斗资源内容

## 使用这个索引时的补充规则

- 顶视角第一优先：
  - `Lana Studio`
  - `Hun0FX`
  - `SF Studio`
  - `Super Pixel` 系列里偏平面的爆点 / 投射物
- 如果看到很立体、很高、很偏演示的 prefab：
  - 默认先拆原料
  - 不要直接整包挂上去
- 如果是像素风和当前主场景风格差异很大：
  - 默认把它当局部子特效
  - 不要第一步就整套直接上场
- 如果包本身带 `ScriptBased`、`Demo`、`URP` 明显标记：
  - 默认先检查依赖、shader 和视角
  - 不要假设能直接无改动投入当前战斗

## 什么时候应该先做项目专用 prefab

以下情况，默认优先先做项目专用 prefab：

- 资源包里的 prefab 视角不对
- 资源包里的 prefab 层级不对，容易挡住英雄
- 资源包里的 prefab 尺寸、密度、范围不适合当前战斗阅读
- 你已经知道这个效果会长期使用，而不是一次性试验
- 后面大概率还要继续微调美术表现

这样做的好处：

- 最终特效更贴合本项目
- 后续视觉微调更直观
- 代码量更容易收敛
- 程序侧引用关系更稳定

## 什么时候才应该补代码

以下情况才补代码，而且优先补“薄的通用代码”：

- 需要根据技能半径动态缩放
- 需要根据持续时间做淡入淡出
- 需要根据 tick 节奏做脉冲反馈
- 需要根据飞行方向摆放弹头和拖尾
- 需要统一控制排序层级，保证英雄永远在上层

规则：

- 不要把“做特效”默认等同于“写一大段专用 controller”
- 代码只负责 prefab 自己做不到的逻辑联动
- 如果某段表现代码未来只会服务一个短期试验效果，应优先考虑先把视觉沉淀回 prefab

## 技能区域特效的当前规则

`SkillData` 当前与区域特效表现直接相关的字段如下：

- `persistentAreaVfxPrefab`
- `persistentAreaVfxScaleMultiplier`
- `persistentAreaVfxEulerAngles`
- `skillAreaPresentationType`

它们的职责分别是：

- `persistentAreaVfxPrefab`
  - 当 `skillAreaPresentationType = None` 时，表示直接实例化的区域 prefab
  - 当使用自定义 controller 时，表示供 controller 复用的资源来源，不一定等于“最终整体效果 prefab”
- `persistentAreaVfxScaleMultiplier`
  - 表现层缩放修正参数
  - 只应用于视觉，不改变逻辑判定半径
- `persistentAreaVfxEulerAngles`
  - 表现层额外旋转参数
  - 只在直接实例化 prefab 的路径下生效
- `skillAreaPresentationType`
  - 决定当前技能区域是走“直接 prefab 路径”还是“自定义 controller 路径”

`RuntimeSkillArea` 当前真正影响技能区域逻辑的，是这些运行时参数：

- `Radius`
- `TotalDurationSeconds`
- `RemainingDurationSeconds`
- `TickIntervalSeconds`
- `CurrentCenter`

规则：

- 表现层必须消费这些正式逻辑参数
- 表现层不能反过来决定判定范围、持续时间或伤害生效时机

当前已有的区域表现类型：

- `None`
- `FireSea`

当前区域技能相关排序规则：

- 区域底圈：`SkillAreaCircleSortOrder`
- 区域特效：`SkillAreaEffectSortOrder`
- 英雄：整体在技能区域之上

结论：

- 持续区域技能默认不要长期走“单个现成 prefab 直接放大”的路线
- 如果效果已经稳定，应优先沉淀为项目专用 prefab
- 如果还需要逻辑参数驱动，再补一层薄的通用驱动

## 普通攻击飞行物的当前规则

`HeroVisualConfig` 当前与普通攻击飞行物表现直接相关的字段包括：

- `projectilePrefab`
- `projectileAlignToMovement`
- `projectileEulerAngles`
- `hitVfxPrefab`
- 后续若需要扩展，可继续通过 hero visual config 增加表现资源入口

`RuntimeBasicAttackProjectile` 当前真正驱动飞行物逻辑的核心参数包括：

- `CurrentPosition`
- `Target`
- `Speed`

规则：

- 普攻的命中时间、速度、目标追踪仍由战斗逻辑决定
- 表现层不能自行改伤害、改命中时机、改飞行速度
- 对顶视角战斗，优先让 prefab 或薄驱动去适配逻辑轨迹，而不是反过来让特效决定轨迹

补充说明：

- `hitVfxPrefab` 当前更适合作为“默认目标命中特效”这类按英雄表现配置的资源入口
- `BattleView` 在收到 `HealAppliedEvent` 时，当前优先尝试播放共享资源路径 `Assets/Resources/Stage01Demo/VFX/Shared/HealReceivedImpact.prefab`
- 这条路径用于后续所有“英雄受到治疗”时统一复用同一套身上闪效，不要求每个治疗英雄都单独配置自己的受治疗特效

## 护盾与治疗表现的当前规则

当前第一阶段把“护盾”和“受治疗反馈”区分为两类不同表现：

- `护盾`
  - 默认优先做成 HUD/脚下 UI 可读信息，而不是角色身上的长期环绕特效
  - 当前规则是：盾量条从当前生命填充的右端开始绘制，未满血时先占用血条里剩余空段；只有超出满血边界的那部分，才继续向血条右侧外延
  - 盾量条长度仍按当前盾量相对最大生命值换算
  - 盾量来源仍完全由 `StatusEffectType.Shield` 的运行时剩余值决定
- `治疗`
  - 默认优先做成角色身上的短时 one-shot 特效
  - 表现层只在收到 `HealAppliedEvent` 时播放，不反向决定治疗是否生效、治疗数值或治疗时机

对后续 AI 的要求：

- 如果只是普通护盾，不要默认再做一个角色环绕 loop 特效
- 如果后续英雄也要复用治疗身上特效，优先继续走“共享 runtime prefab + `HealAppliedEvent` 统一触发”的路径
- 如果某个护盾确实需要额外辨识度，再在“盾量条”之上补少量附加表现，而不是拿附着特效替代盾量读数

## 单位附着状态特效的当前规则

当前第一阶段已经开始接入“跟随英雄本体的状态特效”，用于让硬控或关键状态在顶视角战斗里能被快速识别。

当前规则：

- 状态逻辑是否存在、何时结束，仍完全由 `StatusEffectSystem` 和运行时状态列表决定
- 表现层只根据单位当前 `ActiveStatusEffects` 去创建、同步和移除对应 VFX
- 不要在技能逻辑里额外手写“眩晕时播放某个特效”的一次性特例
- 单位附着状态特效如果需要运行时自动加载，当前优先放在 `game/Assets/Resources/Stage01Demo/VFX/Statuses/`
- 即使放在 `Resources/` 路径下，也应优先使用“项目内整理后的 prefab”，不要直接长期引用资源包原始路径

当前已接入示例：

- `眩晕`
  - 项目内运行时 prefab：`game/Assets/Resources/Stage01Demo/VFX/Statuses/StunStatusLoop.prefab`
  - 素材来源：`Assets/Lana Studio/Casual RPG VFX/Prefabs/States/Stun_loop.prefab`
  - 接入方式：`BattleView` 在英雄视图下创建并跟随该状态 VFX，状态结束后自动销毁

对后续 AI 的要求：

- 后续再加 `KnockUp`、`Invulnerable`、`Untargetable` 等状态表现时，优先沿用同一条“统一状态 VFX 映射”路径
- 如果某个状态要长期存在于项目里，先整理成项目内 prefab，再接运行时映射
- 状态 VFX 只能表达“当前单位身上有什么状态”，不能反向决定状态持续时间、控制时长或行为门禁

## 后续新增一个区域技能特效时的标准流程

### 1. 先判断技能语义

先确认它是以下哪种：

- 单点爆发
- 持续地面区域
- 多段落点区域
- 护盾 / aura 类包裹表现
- 跟随施法者的持续区域

### 2. 优先先做项目专用 prefab

优先把以下内容整理进 prefab：

- 地面底板
- 局部火簇或局部子特效
- 层级关系
- 动画节奏

不要先拿资源包里看起来顺眼的单个 prefab 直接硬套。

### 3. 再决定程序侧只需要多薄的一层

如果 prefab 已经足够表达视觉：

- 程序只负责实例化、摆放和销毁

如果还需要逻辑联动：

- 再补一个尽量通用的 area-vfx 驱动层

### 4. SkillData 资产和 builder 必须同步

至少同步这些内容：

- `targetType`
- `areaRadius`
- `persistentAreaVfxPrefab`
- `persistentAreaVfxScaleMultiplier`
- `persistentAreaVfxEulerAngles`
- `skillAreaPresentationType`
- `Stage01SampleContentBuilder.cs`

### 5. 做最小验证

至少检查：

- 技能实际落点对不对
- 视觉范围是否大致覆盖逻辑范围
- 英雄是否仍然在特效上层
- 持续时间是否与技能配置一致
- tick 节奏是否与技能效果一致

## 后续新增一个普通攻击飞行物特效时的标准流程

### 1. 先判断飞行物语义

先确认它是以下哪种：

- 单发箭矢
- 魔法弹
- 火球
- 带拖尾的投射物
- 多段或分裂飞行物

### 2. 优先先做项目专用 prefab

优先把以下内容整理进 prefab：

- 弹头
- 拖尾
- 光晕
- 余烬
- 层级结构

不要第一步就去写一大段飞行物专用代码。

### 3. 再决定程序侧只需要多薄的一层

如果 prefab 已经足够表达视觉：

- 程序只负责实例化、移动和销毁

如果还需要逻辑联动：

- 再补一个尽量通用的 projectile 驱动层

### 4. 资产和 builder 必须同步

至少同步：

- 英雄表现配置资产
- `Stage01SampleContentBuilder.cs`

### 5. 做最小验证

至少检查：

- 飞行方向是否可读
- 大小是否适合顶视角战斗
- 没有遮住英雄本体
- 飞行速度和命中时机没有因为表现层被改坏

## 当前火法大招的理解方式

当前 `Meteor Fall` 应按以下方式理解：

- 逻辑层
  - 朝敌人最密集区域施放
  - 范围半径 `6`
  - 持续 `5 秒`
  - 每 `1 秒` tick 一次
- 表现层
  - 当前直接使用项目专用 prefab：`Assets/Prefabs/VFX/Skills/FireMageMeteorField.prefab`
  - 资源包里的火焰 prefab 只作为该 prefab 的素材来源
  - 程序侧只保留通用区域缩放、排序和生命周期控制

## 当前火法小技能的理解方式

当前 `Ember Burst` 应按以下方式理解：

- 逻辑层
  - 朝敌人较密集区域施放
  - 技能范围半径仍为 `2`
  - 持续时间当前为 `0.4 秒`
  - tick 频率和伤害逻辑仍由技能数据驱动
- 表现层
  - 当前直接使用项目专用 prefab：`Assets/Prefabs/VFX/Skills/FireMageEmberBurst.prefab`
  - 资源包里的 fire burst、explosion、fire trail 只作为该 prefab 的素材来源
  - 程序侧只保留通用区域缩放、排序和生命周期控制

## 当前火法普攻的理解方式

当前 `FIREMAGE` 普攻应按以下方式理解：

- 逻辑层
  - 仍是普通攻击投射物
  - 命中判定、目标追踪、速度仍由 `BattleBasicAttackSystem` 驱动
  - 当前 projectile speed 为 `14`
- 表现层
- 当前直接使用项目专用 prefab：`Assets/Prefabs/VFX/Projectiles/FireMageBasicAttackProjectile.prefab`
- 程序侧只保留通用的移动同步、排序和按方向旋转
- 资源包里的 missile、spark、fire trail prefab 只作为该 prefab 的素材来源

## 当前圣职者治疗表现的理解方式

当前 `Sunpriest` 应按以下方式理解：

- 逻辑层
  - 普攻仍是治疗型普通攻击投射物
  - 小技能仍是单体治疗 + 护盾
  - 大招仍是持续范围治疗
- 表现层
  - 普攻飞行物使用项目专用 prefab：`Assets/Prefabs/VFX/Projectiles/SunpriestBasicAttackProjectile.prefab`
  - 受治疗身上闪效的工程源 prefab 当前仍整理在：`Assets/Prefabs/VFX/Shared/SunpriestHealImpact.prefab`
  - 运行时统一复用的受治疗闪效当前由：`Assets/Resources/Stage01Demo/VFX/Shared/HealReceivedImpact.prefab` 提供
  - 护盾不默认做角色附着 loop，而是通过 `BattleView` 的血条右侧盾量段表现
  - 大招范围治疗使用项目专用 prefab：`Assets/Prefabs/VFX/Skills/SunpriestSunBlessingField.prefab`

## 常见问题定位顺序

### 1. 看起来范围不对

先检查：

- 技能是否真的落在正确位置
- `targetType` 是否正确
- `ultimateDecision.targetingType` 是否正确
- `areaRadius` / `radiusOverride` 是否正确

不要第一反应先改缩放倍率。

### 2. 看起来里面不够满

优先检查：

- prefab 本身的铺满程度
- 子特效分布
- 底板层次
- 局部火簇尺寸

不要先改逻辑半径。

### 3. 特效盖住英雄

优先检查：

- `BattleView` 里的固定排序
- prefab 或驱动层是否把某些 renderer 单独拉到了更高层

不要靠随便改英雄排序去兜底。

### 4. 看起来太立体、太像侧面

优先判断：

- 这个资源包 prefab 是否本来就是竖向体积效果
- 它是否根本不适合作为顶视角最终表现

如果答案是“是”：

- 优先重组项目专用 prefab
- 不要长期依赖“多试几个旋转角度”硬救

### 5. 代码又开始变多了

优先检查：

- 这次需求是否本来就应该先沉淀成 prefab
- 是不是把“做表现”误做成了“再写一个英雄专用 controller”
- 能不能把现在的表现结构回收进 `Assets/Prefabs/VFX/`

规则：

- 如果最终效果已经稳定，优先把它整理成项目专用 prefab
- 英雄专属的大段表现代码不应成为默认长期方案

### 6. prefab 生成命令跑不起来

优先检查：

- Unity 工程是不是已经在另一个编辑器实例里打开
- 是不是误把“要重建 prefab”理解成“只能跑 batchmode”

处理顺序：

- 如果项目已打开，先用当前编辑器里的 `Fight -> Stage 01 -> Build FireMage VFX Prefabs`
- 如果需要批处理重建，再先关闭当前 Unity 编辑器

## 与外部 Codex skill 的关系

如果当前 Codex 环境里可用 `fight-unity-vfx-prefab-first` 这个 skill：

- 可把它理解为这份文档的外部执行包装
- 它的目标是把这里的 prefab-first 流程更稳定地应用到具体 VFX 任务上

如果当前环境里没有这个 skill：

- 直接按本文档执行即可
- 仓库本身不依赖这个外部 skill 才能运行

## 给后续 AI 的执行要求

后续再做新的战斗特效时，默认按以下顺序：

1. 先确认逻辑参数是否正确
2. 再决定资源包里的内容只是原料还是可以直接复用
3. 优先产出项目专用 prefab
4. 改完资产后，同步更新 `Stage01SampleContentBuilder.cs`
5. 最后才决定是否需要补一层薄的通用代码

一句话要求：

- 默认先产出项目专用 prefab，再由程序调用它。
