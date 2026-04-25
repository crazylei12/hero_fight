# 第一阶段战斗特效表现使用说明

最后更新：2026-04-25

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

1. 先问清楚这个特效是给什么东西用的，以及用户想要什么样的效果
2. 再确认它在战斗里的语义，以及必须对齐的逻辑参数、范围和时序
3. 先查现有项目 prefab 和素材库里有没有合适原料，判断哪些能直接复用、哪些适合组合
4. 先把候选方案或组合思路推荐给用户检查；如果没有合适素材，先说明打算怎么做、预期会是什么效果
5. 用户确认方向后，再组装“项目专用 prefab”
6. 只有当 prefab 本身无法表达逻辑联动时，才补一层很薄的通用驱动代码
7. 最后由程序去调用这个项目专用 prefab，而不是直接调用资源包原始 prefab

默认规则：

- 先确认需求，再找素材，再推荐方案，最后动手
- 用户还没确认方向时，不要因为一句“做个特效”就直接开做
- 资源包是原料，不是最终成品
- 少写英雄专属的大段表现代码
- 真需要代码时，也优先写可复用的通用层
- 单体技能如果只需要“施法瞬间在主目标处播放一次斩击/命中特效”，优先使用 `SkillData.castImpactVfxPrefab` 这类通用瞬时挂点，不要为了单个英雄再写专属表现脚本
- 如果某个瞬发技能本身是“以施法者或落点为中心的一次性范围爆发 / 治疗脉冲”，也可以继续走 `SkillData.castImpactVfxPrefab`；这类情况下优先通过通用字段让表现尺寸跟 `areaRadius` 绑定，而不是把逻辑硬改成持续区域技能

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
- 编辑器菜单：`Fight -> Stage 01 -> Build Shared Dash Charge VFX`
- batchmode 执行方法：`Fight.Editor.SharedDashChargeVfxPrefabBuilder.BuildSharedDashChargeVfxBatch`
- 编辑器菜单：`Fight -> Stage 01 -> Build Pythoness Shrinemaiden VFX`
- batchmode 执行方法：`Fight.Editor.PythonessVfxPrefabBuilder.BuildPythonessVfxPrefabsBatch`

当前这条 builder 已覆盖：

- 火法普攻投射物
- 火法小技能 `Ember Burst`
- 火法大招 `Meteor Fall`
- 共享冲锋拖尾 `DashChargeTrail`
- 巫女普攻伤害 / 治疗投射物与命中闪效
- 巫女小技能 `Prayer Bloom` 的范围瞬时特效
- 巫女大招 `Twin Rite Totem` 的出现、循环、消失三段部署物特效

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
- `game/Assets/Scripts/Editor/SharedDashChargeVfxPrefabBuilder.cs`
- `game/Assets/Scripts/Editor/PythonessVfxPrefabBuilder.cs`
- `game/Assets/Scripts/UI/Preview/SpriteTextureFrameAnimator.cs`
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

当前巫女配置资产：

- `game/Assets/Data/Stage01Demo/Heroes/support_004_shrinemaiden/Shrinemaiden.asset`
- `game/Assets/Data/Stage01Demo/Skills/support_004_shrinemaiden/Prayer Bloom.asset`
- `game/Assets/Data/Stage01Demo/Skills/support_004_shrinemaiden/Twin Rite Totem.asset`

当前巫女 VFX prefab：

- `game/Assets/Prefabs/VFX/Projectiles/ShrinemaidenDamageProjectile.prefab`
- `game/Assets/Prefabs/VFX/Projectiles/ShrinemaidenHealProjectile.prefab`
- `game/Assets/Prefabs/VFX/Shared/ShrinemaidenDamageImpact.prefab`
- `game/Assets/Prefabs/VFX/Shared/ShrinemaidenHealImpact.prefab`
- `game/Assets/Prefabs/VFX/Skills/ShrinemaidenPrayerBloomImpact.prefab`
- `game/Assets/Prefabs/VFX/Skills/ShrinemaidenTotemSpawn.prefab`
- `game/Assets/Prefabs/VFX/Skills/ShrinemaidenTotemLoop.prefab`
- `game/Assets/Prefabs/VFX/Skills/ShrinemaidenTotemDisappear.prefab`

当前巫女像素图接入规则：

- 角色模型仍使用 `support_004_shrinemaiden/Shrinemaiden.prefab`，不要把 `mage_004_pythoness` 预览 prefab 当正式巫女本体
- `pythoness__38.png` / `pythoness_effect__73.png` 只作为特效帧素材来源，实际运行引用整理后的项目 prefab
- 部署物如果需要进入 / 循环 / 离场三段表现，优先使用 `SkillEffectData.deployableProxySpawnVfxPrefab`、`deployableProxyLoopVfxPrefab`、`deployableProxyRemovalVfxPrefab`，不要写英雄专属的播放分支

## 当前资源包速查索引

更详细的人工校准记录见：`docs/planning/stage-01-vfx-asset-library-catalog.md`。如果用户已经在该文件中确认过某个资源库的实际内容，后续 AI 判断素材候选时应优先参考该文件，再结合本节的快速索引做路径检索。

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

- `castProjectileVfxPrefab`
- `persistentAreaVfxPrefab`
- `persistentAreaVfxScaleMultiplier`
- `persistentAreaVfxEulerAngles`
- `skillAreaPresentationType`

它们的职责分别是：

- `castProjectileVfxPrefab`
  - 用于“技能先抛出一个可见投掷物，再在目标点结算”的表现入口
  - 当前主要服务 `ThrownProjectile` 这类共享投掷表现，不参与逻辑命中、时机和伤害判定
- `persistentAreaVfxPrefab`
  - 当 `skillAreaPresentationType = None` 时，表示直接实例化的区域 prefab
  - 当使用自定义 controller 时，表示供 controller 复用的资源来源，不一定等于“最终整体效果 prefab”
  - 对 `ThrownProjectile` 这类延迟落地爆发技能，当前默认作为“落地爆炸 one-shot prefab”来消费
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
- `ThrownProjectile`

`ThrownProjectile` 当前的约定是：

- 逻辑层仍然沿用正式 `RuntimeSkillArea` 的落点、持续时间和脉冲时机
- 表现层只负责从施法者可用锚点抛出 `castProjectileVfxPrefab`，并在脉冲发生时于落点播放 `persistentAreaVfxPrefab`
- 这条通路是共享投掷表现层，不要把它实现成某个英雄私有的“手雷专属底层”

当前区域技能相关排序规则：

- 区域底圈：`SkillAreaCircleSortOrder`
- 区域特效：`SkillAreaEffectSortOrder`
- 英雄：整体在技能区域之上

结论：

- 持续区域技能默认不要长期走“单个现成 prefab 直接放大”的路线
- 如果效果已经稳定，应优先沉淀为项目专用 prefab
- 如果还需要逻辑参数驱动，再补一层薄的通用驱动

## 技能目标锁定表现的当前规则

当前第一阶段也开始正式支持“技能已经锁定某个直接目标”这类短时提示，不只服务 `Rifleman`。

当前规则：

- `SkillData.targetIndicatorVfxPrefab`
  - 用于“这个技能在释放或连续动作期间，需要给主目标挂一个短时锁定提示”的表现入口
  - 当前优先服务 `Burst Fire` 这类单体连射技能，也适合后续的点名狙击、标记射击、处决前摇等共享语义
- 目标指示特效应优先做成项目内 prefab，并继续放在 `Assets/Prefabs/VFX/Skills/` 或其他合适的项目目录，不要直接长期引用资源包原始 prefab
- `BattleView` 当前会在收到 `SkillCastEvent` 时，把这套 prefab 挂到 `PrimaryTarget` 身上
- 如果该技能还带 `actionSequence`，当前默认让这个提示的可见时长大致覆盖整段连续动作，而不是只闪一下
- 这条提示只表达“当前正在被这个技能锁定或压制”，不反向决定目标是否合法、伤害是否命中、连射是否继续

对后续 AI 的要求：

- 如果某个技能只是需要“点名提示”，优先复用这条 `targetIndicatorVfxPrefab` 路径，不要再写新的英雄专属 BattleView 分支
- 如果后续某个锁定提示需要更长时长或更复杂编排，优先先看能不能继续收敛在 prefab 结构或共享续时逻辑里
- 不要把这类视觉提示误做成状态系统或命中判定的一部分

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

## 瞬移闪现表现的当前规则

当前第一阶段把“零时长瞬移切位”的闪现反馈也收敛为一条共享表现语义，不只服务 `Shadowstep`：

- 逻辑层仍完全由 `SkillType.Dash` 和 `ForcedMovementAppliedEvent` 决定
- 当一次位移同时满足以下条件时，表现层默认把它视为 `瞬移 / 闪现`
  - 来源技能是 `Dash`
  - 位移对象是施法者自己
  - 位移时长约等于 `0`
- 表现层会在 `起点` 生成一次角色残影快照并快速淡出
- 英雄到达 `终点` 后，会先以短暂半透明状态出现，再恢复为正常实体
- 到达点还会额外叠一层更轻的角色残影，帮助读出“先虚化、再显形”的节奏
- 这条反馈当前优先复用 `BattleView` 中的共享角色快照逻辑，而不是额外依赖单独 prefab

对后续 AI 的要求：

- 以后再做“瞬移到目标身边”“短距离闪现”“零时长 re-position”这类技能时，优先复用这条共享角色残影 + 淡入逻辑
- 有明显飞行时间或滑步过程的 dash，继续优先沿用统一 `ForcedMovement` 位移表现，不要强行套 blink prefab
- 不要在单个英雄脚本里额外手写“旧位置播一次、新位置再显形”的专用闪现逻辑

## 冲锋突进表现的当前规则

当前第一阶段把“有明显位移过程的冲锋 / 滑步 dash”也收敛为一条共享表现语义，不只服务 `Skybreaker`：

- 逻辑层仍完全由 `SkillType.Dash` 和 `ForcedMovementAppliedEvent` 决定
- 当一次位移同时满足以下条件时，表现层默认把它视为 `冲锋 / 突进`
  - 来源技能是 `Dash`
  - 位移对象是施法者自己
  - 位移时长明显大于 `0`
- 默认情况下，表现层会在位移开始时给施法者挂上一段共享 follow-trail prefab，并按冲锋方向旋转
- 如果某个 dash 技能需要“人物前方顶着一段剑气 / 波锋一起前冲”的读图方式，可直接在 `SkillData` 上配置：
  - `dashTravelVfxPrefab`
  - `dashTravelVfxLocalOffset`
  - `dashTravelVfxForwardOffset`
  - `dashTravelVfxEulerAngles`
  - `dashTravelVfxScaleMultiplier`
  - `dashTravelVfxPathWidthScaleMultiplier`
- 当 `dashTravelVfxPrefab` 存在时，`BattleView` 当前会把这套 prefab 跟随挂到施法者身上，沿 dash 方向旋转，并按技能 `DashPathEnemies` 的逻辑路径宽度去放大横向宽度
- 这条 prefab 只表达“正在冲锋”，不反向修改落点、时序、伤害或路径命中
- 项目工程源 prefab 当前整理在：`game/Assets/Prefabs/VFX/Shared/DashChargeTrail.prefab`
- 运行时统一加载路径当前为：`game/Assets/Resources/Stage01Demo/VFX/Shared/DashChargeTrail.prefab`

对后续 AI 的要求：

- 以后再做“短冲锋切入”“滑步贴脸”“带过程时间的 dash”时，优先复用这条共享 `DashChargeTrail` 路径
- 如果想要的是“顶在角色前方一起飞的波锋 / 剑气”，优先走 `SkillData.dashTravelVfxPrefab` 这条共享 dash-follow 通路，不要单独再写英雄私有表现脚本
- 如果是 `零时长` 的瞬移切位，继续走共享角色残影 + 淡入逻辑，不要和冲锋拖尾混着播
- 不要在单个英雄脚本里额外手写“冲锋时挂一个 trail”的专用表现逻辑

## 部署型代理体表现的当前规则

当前第一阶段开始支持“部署型固定战斗代理体”的可见 loop 表现，用于巫女 `Twin Rite Totem` 这类不会成为完整英雄、但需要在战场上被读出来的短时召唤物。

当前规则：

- 代理体的创建、持续时间、攻击频率、目标选择和移除仍完全由 `RuntimeDeployableProxy` 与 `BattleDeployableProxySystem` 决定
- 表现层只读取 `SkillEffectData.deployableProxyLoopVfxPrefab` 及其 offset / rotation / scale 字段来创建和同步可见对象
- 代理体 VFX 应继续使用项目内整理过的 prefab，例如 `game/Assets/Prefabs/VFX/Skills/ShrinemaidenTotemLoop.prefab`
- 这类 VFX 只能表达“这里有一个正在生效的代理体”，不能反向决定代理体持续时间、攻击时机、目标或数值
- 如果后续需要生成 / 消失瞬间特效，优先继续扩展 `SkillEffectData` 的通用表现字段，不要为单个英雄在 `BattleView` 里写硬编码分支

当前已接入示例：

- `Twin Rite Totem`
  - 项目内工程 prefab：`game/Assets/Prefabs/VFX/Skills/ShrinemaidenTotemLoop.prefab`
  - 素材来源：`pythoness__38.png` 拆出的 `DoorLoop` 像素序列
  - 接入方式：`BattleView` 在 `context.DeployableProxies` 中发现带有 `deployableProxyLoopVfxPrefab` 的代理体后创建、跟随并在代理体消失时清理对应 loop VFX

## 单位附着状态特效的当前规则

当前第一阶段已经开始接入“跟随英雄本体的状态特效”，用于让硬控或关键状态在顶视角战斗里能被快速识别。

当前规则：

- 状态逻辑是否存在、何时结束，仍完全由 `StatusEffectSystem` 和运行时状态列表决定
- 表现层只根据单位当前 `ActiveStatusEffects` 去创建、同步和移除对应 VFX
- 不要在技能逻辑里额外手写“眩晕时播放某个特效”的一次性特例
- 单位附着状态特效如果需要运行时自动加载，当前优先放在 `game/Assets/Resources/Stage01Demo/VFX/Statuses/`
- 即使放在 `Resources/` 路径下，也应优先使用“项目内整理后的 prefab”，不要直接长期引用资源包原始路径
- 对同一个 `StatModifier` 枚举，如果正负方向需要不同视觉语义，优先按该状态当前合并后的实际数值方向决定显示哪套 VFX，不要只按 `StatusEffectType` 名称硬绑一套表现

当前已接入示例：

- `眩晕`
  - 项目内运行时 prefab：`game/Assets/Resources/Stage01Demo/VFX/Statuses/StunStatusLoop.prefab`
  - 素材来源：`Assets/Lana Studio/Casual RPG VFX/Prefabs/States/Stun_loop.prefab`
  - 接入方式：`BattleView` 在英雄视图下创建并跟随该状态 VFX，状态结束后自动销毁
- `击飞`
  - 项目内运行时 prefab：`game/Assets/Resources/Stage01Demo/VFX/Statuses/KnockUpStatusBurst.prefab`
  - 素材来源：`Assets/Lana Studio/Casual RPG VFX/Prefabs/Burst/Burst_rings.prefab`
  - 接入方式：`BattleView` 通过统一状态 VFX 映射在击飞开始时创建 one-shot 爆圈，并随状态生命周期自动清理
- `嘲讽`
  - 项目内工程 prefab：`game/Assets/Prefabs/VFX/Shared/TauntStatusLoop.prefab`
  - 项目内运行时 prefab：`game/Assets/Resources/Stage01Demo/VFX/Statuses/TauntStatusLoop.prefab`
  - 图标素材来源：`game/Assets/Art/VFX/StatusIcons/TauntEffect.png`
  - 接入方式：`BattleView` 对 `StatusEffectType.Taunt` 直接创建这套“头顶浮动 taunt 图标”状态特效，让被嘲讽单位在顶视角里能快速读出“当前被强制转火”
- `攻击力下降`
  - 项目内工程 prefab：`game/Assets/Prefabs/VFX/Shared/AttackPowerDownStatusLoop.prefab`
  - 项目内运行时 prefab：`game/Assets/Resources/Stage01Demo/VFX/Statuses/AttackPowerDownStatusLoop.prefab`
  - 图标素材来源：`game/Assets/Art/VFX/StatusIcons/AttackDebuffEffect.png`
  - 接入方式：`BattleView` 对 `StatusEffectType.AttackPowerModifier` 先合并当前英雄身上的总修正值；只有合并后仍为负数时，才创建这套“图标绕英雄躯干前后环绕”的减攻状态特效
- `攻击力上升`
  - 项目内工程 prefab：`game/Assets/Prefabs/VFX/Shared/AttackPowerUpStatusLoop.prefab`
  - 项目内运行时 prefab：`game/Assets/Resources/Stage01Demo/VFX/Statuses/AttackPowerUpStatusLoop.prefab`
  - 图标素材来源：`game/Assets/Art/VFX/StatusIcons/AttackBuffEffect.png`
  - 接入方式：`BattleView` 对 `StatusEffectType.AttackPowerModifier` 先合并当前英雄身上的总修正值；只有合并后仍为正数时，才创建这套“图标绕英雄躯干前后环绕”的加攻状态特效
- `防御力下降`
  - 项目内工程 prefab：`game/Assets/Prefabs/VFX/Shared/DefenseDownStatusLoop.prefab`
  - 项目内运行时 prefab：`game/Assets/Resources/Stage01Demo/VFX/Statuses/DefenseDownStatusLoop.prefab`
  - 图标素材来源：`game/Assets/Art/VFX/StatusIcons/DefenceDebuffEffect.png`
  - 接入方式：`BattleView` 对 `StatusEffectType.DefenseModifier` 先合并当前英雄身上的总修正值；只有合并后仍为负数时，才创建这套“图标绕英雄躯干前后环绕”的减防状态特效
- `防御力上升`
  - 项目内工程 prefab：`game/Assets/Prefabs/VFX/Shared/DefenseUpStatusLoop.prefab`
  - 项目内运行时 prefab：`game/Assets/Resources/Stage01Demo/VFX/Statuses/DefenseUpStatusLoop.prefab`
  - 图标素材来源：`game/Assets/Art/VFX/StatusIcons/DefenceBuffEffect.png`
  - 接入方式：`BattleView` 对 `StatusEffectType.DefenseModifier` 先合并当前英雄身上的总修正值；只有合并后仍为正数时，才创建这套“图标绕英雄躯干前后环绕”的加防状态特效
- `攻速上升 / 下降`
  - 项目内工程 prefab：`game/Assets/Prefabs/VFX/Shared/AttackSpeedUpStatusLoop.prefab`、`game/Assets/Prefabs/VFX/Shared/AttackSpeedDownStatusLoop.prefab`
  - 项目内运行时 prefab：`game/Assets/Resources/Stage01Demo/VFX/Statuses/AttackSpeedUpStatusLoop.prefab`、`game/Assets/Resources/Stage01Demo/VFX/Statuses/AttackSpeedDownStatusLoop.prefab`
  - 图标素材来源：`game/Assets/Art/VFX/StatusIcons/AttackSpeedBuffEffect.png`、`game/Assets/Art/VFX/StatusIcons/AttackSpeedDebuffEffect.png`
  - 接入方式：`BattleView` 对 `StatusEffectType.AttackSpeedModifier` 先合并当前英雄身上的总修正值；按正负方向自动在加攻速 / 减攻速图标之间切换
- `移速上升 / 下降`
  - 项目内工程 prefab：`game/Assets/Prefabs/VFX/Shared/MoveSpeedUpStatusLoop.prefab`、`game/Assets/Prefabs/VFX/Shared/MoveSpeedDownStatusLoop.prefab`
  - 项目内运行时 prefab：`game/Assets/Resources/Stage01Demo/VFX/Statuses/MoveSpeedUpStatusLoop.prefab`、`game/Assets/Resources/Stage01Demo/VFX/Statuses/MoveSpeedDownStatusLoop.prefab`
  - 图标素材来源：`game/Assets/Art/VFX/StatusIcons/MoveSpeedBuffEffect.png`、`game/Assets/Art/VFX/StatusIcons/MoveSpeedDebuffEffect.png`
  - 接入方式：`BattleView` 对 `StatusEffectType.MoveSpeedModifier` 先合并当前英雄身上的总修正值；按正负方向自动在加移速 / 减移速图标之间切换
- 属性增减类图标状态的当前共用规则：
  - 这类图标优先共用同一条“绕躯干前后环绕”的身体轨道，而不是每个图标各自独立在平面上转圈
  - 同一英雄同时存在多个这类图标状态时，由 `BattleView` 按当前激活数量自动分配轨道相位，让它们沿同一圈轨道错位分布并一起旋转
  - 同一个 `StatusEffectType` 如果合并后的总修正值在正负之间翻转，表现层应同步重建并切换到对应的 buff / debuff prefab，不要继续沿用旧图标
  - 轨道半径、前后缩放、后半圈透明度等参数优先收敛在统一脚本里，不要为每个属性效果各写一套独立旋转逻辑

## 强制位移 / 击退表现的当前规则

当前第一阶段把“水平击退 / 拉拽这类强制位移表现”单独看作一条共享表现通路，而不是再额外扩一个新的 `StatusEffectType`。

当前规则：

- 强制位移的逻辑是否生效、持续多久、把单位带到哪里，仍完全由 `SkillEffectType.ApplyForcedMovement`、`RuntimeHero.IsUnderForcedMovement` 和运行时位移过程决定
- 表现层只读取 `ForcedMovementAppliedEvent` 给出的方向 / 距离 / 时长，再叠加当前英雄是否仍处于强制位移中
- 不要在 `Longshot` 这类具体英雄里再手写一次“被击退时播放特效”的专属分支
- 只要是明显的水平强制位移，默认都应复用同一套共享击退表现

当前已接入示例：

- `击退 / 水平强制位移`
  - 项目内工程 prefab：`game/Assets/Prefabs/VFX/Shared/KnockbackStatusLoop.prefab`
  - 项目内运行时 prefab：`game/Assets/Resources/Stage01Demo/VFX/Statuses/KnockbackStatusLoop.prefab`
  - 构建入口：`game/Assets/Scripts/Editor/StatusVfxPrefabBuilder.cs`
  - 接入方式：`BattleView` 在检测到英雄处于 `IsUnderForcedMovement` 且存在明显水平位移时，自动创建并跟随这套共享 loop 特效；特效会按位移方向旋转，并在位移结束后自动移除

对后续 AI 的要求：

- 后续再加 `Invulnerable`、`Untargetable` 等状态表现时，优先沿用同一条“统一状态 VFX 映射”路径
- 如果某个状态要长期存在于项目里，先整理成项目内 prefab，再接运行时映射
- 状态 VFX 只能表达“当前单位身上有什么状态”，不能反向决定状态持续时间、控制时长或行为门禁

## 后续新增一个区域技能特效时的标准流程

### 1. 先确认这是给什么做的，以及想要什么效果

至少先明确：

- 这是普攻、小技能、大招、状态，还是共享表现部件
- 用户更在意的观感是爆发、持续压场、警示、护盾感、命中感，还是其他阅读重点
- 有没有必须保留的范围感、时机感或顶视角可读性要求

### 2. 再判断技能语义和逻辑约束

先确认它是以下哪种：

- 单点爆发
- 持续地面区域
- 多段落点区域
- 护盾 / aura 类包裹表现
- 跟随施法者的持续区域

### 3. 先搜索项目现有 prefab 和素材库

优先检查：

- `game/Assets/Prefabs/VFX/` 下是否已经有可复用的项目专用 prefab
- 本文档里的“当前资源包速查索引”里有没有合适原料
- 哪些素材适合直接复用，哪些更适合拆开重新组合

### 4. 先给用户推荐方案

至少应给出以下两类信息之一：

- 有合适素材时：列出候选来源，并说明打算怎么组合
- 没有合适素材时：说明准备自己怎么做，以及预期会呈现出什么样的效果

用户还没确认方向前，不要直接开始大规模制作。

### 5. 用户确认后，再做项目专用 prefab

优先把以下内容整理进 prefab：

- 地面底板
- 局部火簇或局部子特效
- 层级关系
- 动画节奏

不要先拿资源包里看起来顺眼的单个 prefab 直接硬套。

### 6. 再决定程序侧只需要多薄的一层

如果 prefab 已经足够表达视觉：

- 程序只负责实例化、摆放和销毁

如果还需要逻辑联动：

- 再补一个尽量通用的 area-vfx 驱动层

### 7. SkillData 资产和 builder 必须同步

至少同步这些内容：

- `targetType`
- `areaRadius`
- `persistentAreaVfxPrefab`
- `persistentAreaVfxScaleMultiplier`
- `persistentAreaVfxEulerAngles`
- `skillAreaPresentationType`
- `Stage01SampleContentBuilder.cs`

### 8. 做最小验证

至少检查：

- 技能实际落点对不对
- 视觉范围是否大致覆盖逻辑范围
- 英雄是否仍然在特效上层
- 持续时间是否与技能配置一致
- tick 节奏是否与技能效果一致

## 后续新增一个普通攻击飞行物特效时的标准流程

### 1. 先确认这是给什么飞行物做的，以及想要什么效果

至少先明确：

- 这是英雄普攻、技能投射物，还是共享 projectile 表现部件
- 用户想要的观感更偏箭矢、魔弹、火球、能量束，还是其他读图方向
- 有没有必须保留的速度感、命中感或方向可读性要求

### 2. 再判断飞行物语义

先确认它是以下哪种：

- 单发箭矢
- 魔法弹
- 火球
- 带拖尾的投射物
- 多段或分裂飞行物

### 3. 先搜索项目现有 prefab 和素材库

优先检查：

- `game/Assets/Prefabs/VFX/Projectiles/` 下是否已有可复用 projectile prefab
- 本文档里的“当前资源包速查索引”里有没有合适弹头、拖尾、命中子特效
- 哪些素材适合直接复用，哪些更适合拆开重新组合

### 4. 先给用户推荐方案

至少应给出以下两类信息之一：

- 有合适素材时：列出候选来源，并说明打算怎么组合成 projectile prefab
- 没有合适素材时：说明准备自己怎么做，以及预期会呈现出什么样的效果

用户还没确认方向前，不要直接开始大规模制作。

### 5. 用户确认后，再做项目专用 prefab

优先把以下内容整理进 prefab：

- 弹头
- 拖尾
- 光晕
- 余烬
- 层级结构

不要第一步就去写一大段飞行物专用代码。

### 6. 再决定程序侧只需要多薄的一层

如果 prefab 已经足够表达视觉：

- 程序只负责实例化、移动和销毁

如果还需要逻辑联动：

- 再补一个尽量通用的 projectile 驱动层

### 7. 资产和 builder 必须同步

至少同步：

- 英雄表现配置资产
- `Stage01SampleContentBuilder.cs`

### 8. 做最小验证

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

1. 先问清是给什么做特效，以及想要什么效果
2. 再确认逻辑参数、范围、时序和顶视角可读性约束
3. 先查现有项目 prefab 与素材库，整理候选来源
4. 先把可行方案或组合思路推荐给用户确认；如果没有合适素材，先说明准备怎么做
5. 用户确认后，再产出项目专用 prefab
6. 改完资产后，同步更新 `Stage01SampleContentBuilder.cs`
7. 最后才决定是否需要补一层薄的通用代码

一句话要求：

- 默认先确认需求与方案，再制作项目专用 prefab，再由程序调用它。
