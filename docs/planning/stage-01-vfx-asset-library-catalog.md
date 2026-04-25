# 第一阶段 VFX 素材库人工校准索引

最后更新：2026-04-25

## 文档用途

这份文档用于记录用户亲自浏览资源库后确认的素材内容，目的是减少后续 AI 仅凭文件名、缩略图或资源包分类误判特效用途的问题。

后续 AI 在处理战斗特效时，应先阅读：

- `docs/planning/stage-01-skill-area-presentation-usage.md`
- 本文件

其中 `stage-01-skill-area-presentation-usage.md` 负责说明 prefab-first 工作流程、运行时接入规则和现有 VFX 系统；本文件优先记录“用户实际看过并校准过”的资源库内容，也保留 AI 根据目录结构做出的全量初判标注。

## 记录原则

- 只把用户明确看过、描述过或确认过的素材写入本文件。
- 用户描述优先级高于 AI 对资源包名称、文件名或目录名的猜测。
- AI 可以补充路径、候选用途和风险判断，但必须避免把推断写成用户确认事实。
- AI 根据目录结构做的全量标注必须明确写成“AI 初判”，后续如果用户亲自看过，应再补到“人工校准总览”和“详细记录”里。
- 如果某个素材“看起来能用”，也只表示可作为原料；最终仍优先整理成 `game/Assets/Prefabs/VFX/` 下的项目专用 prefab。
- 如果用户描述不够细，按原话记录，不自行脑补视觉细节。
- 如果素材偏竖向、偏侧视角、遮挡英雄或风格不统一，要在“慎用点”中明确写出。

## 推荐记录格式

后续追加条目时，优先使用以下格式。

### `资源库或目录路径`

- 用户确认内容：
  - 
- 适合用途：
  - 
- 不适合或慎用：
  - 
- 代表素材或子目录：
  - 
- AI 补充判断：
  - 
- 推荐优先级：
  - `高 / 中 / 低 / 待确认`
- 记录日期：
  - `YYYY-MM-DD`

## 标签约定

用途标签可选：

- `projectile`：飞行物、弹体、箭矢、魔法弹
- `impact`：命中点、受击闪光、爆点
- `burst`：瞬时爆发、一次性范围炸开
- `persistent-area`：持续地面区域、火场、毒池、力场
- `aura-shield`：护盾、环绕、角色周身光圈
- `buff-debuff-loop`：持续状态、增益、减益、控制提示
- `slash`：斩击、剑气、爪击
- `dash-trail`：冲锋、位移、拖尾
- `deployable`：图腾、召唤物、固定代理体
- `ultimate-scale`：大招级、压场级、演示级大型技能表现
- `environment`：环境、场景氛围、非技能主效果
- `ui`：界面素材、按钮、面板、窗口、主题 prefab
- `hud`：战斗 HUD、状态栏、行动条、小地图、任务提示等界面 prefab
- `character`：角色、怪物、人物动画或角色 prefab
- `equipment`：武器、装备、饰品、图标
- `tool-plugin`：工具、插件、框架或演示系统
- `project-owned`：项目自有最终资源或运行时资源，不是外部素材包
- `scene-data`：场景、ScriptableObject 数据、项目配置
- `audio`：音频、音效、音乐
- `system`：Unity 系统资源、编辑器资源或第三方基础包

视角与风险标签可选：

- `top-down-friendly`：较适合顶视角
- `side-view`：更偏侧视角
- `vertical-3d`：竖向体积较强，需要拆解或重组
- `pixel-style`：像素风较强，需确认是否和当前角色风格一致
- `blocks-heroes`：可能遮挡英雄
- `too-large`：默认尺寸过大
- `script-based`：依赖脚本或演示逻辑
- `urp-shader-risk`：可能有 URP / shader 兼容风险
- `style-mismatch`：与当前项目风格不稳定
- `needs-prefab-rebuild`：适合作原料，但需要项目专用 prefab 重组
- `uncertain`：还需要用户进一步确认

## `Assets` 顶层目录全量标注（AI 初判）

本节基于 2026-04-25 对 `game/Assets/` 顶层目录、子目录名和资源数量的扫描结果。它是“先别走错门”的地图，不等同于用户逐个看过后的最终校准。

| 顶层目录 | 类型标注 | 默认用途判断 | 后续使用建议 |
| --- | --- | --- | --- |
| `Assets/_TerrainAutoUpgrade/` | `system` | Unity 地形升级辅助目录 | 当前战斗 VFX 搜索时跳过 |
| `Assets/AdventureCreator/` | `tool-plugin` `ui` | 冒险/交互框架，含 demo、UI、prefab 和 resources | 不作为战斗 VFX 来源；除非后续做叙事/交互 UI，否则跳过 |
| `Assets/Art/` | `project-owned` | 项目自有角色、UI、VFX 美术目录 | 这是项目内最终资源或生成资源位置，修改前先确认引用关系 |
| `Assets/AstarPathfindingProject/` | `tool-plugin` | 寻路插件和示例场景 | 不作为美术素材来源 |
| `Assets/Audio/` | `audio` `project-owned` | 项目音频目录 | 与当前 VFX 索材搜索无关 |
| `Assets/CartoonVFX9X/` | `burst` `impact` `persistent-area` | URP 卡通/涂鸦类 VFX 包 | 中优先级候选；先检查 URP/shader 兼容和风格匹配 |
| `Assets/CharacterPack/` | `character` | 角色模型、材质、prefab 和贴图 | 可作角色原型来源，不作为战斗 VFX 首选 |
| `Assets/Data/` | `scene-data` `project-owned` | 项目英雄、技能和阶段数据 | 项目配置数据，不是素材包 |
| `Assets/Editor Default Resources/` | `system` | 编辑器默认资源 | 当前战斗资源搜索时跳过 |
| `Assets/Emerald AI/` | `tool-plugin` `character` | AI 系统、demo、生物或示例资源 | 低优先级；只在需要 AI/demo 示例或个别 projectile 时查看 |
| `Assets/ExplosiveLLC/` | `character` `tool-plugin` | 角色动画、控制器、demo props | 可查动画或演示道具，不作为 VFX 首选 |
| `Assets/Fantasy-Environment/` | `environment` | 建筑、植被、地形 props、火焰/烟/雾等粒子 | 适合战场背景和环境补充；技能 VFX 只取粒子原料 |
| `Assets/FantasyMonsters/` | `character` | 怪物角色、动画、UI 和 sprites | 可作怪物/召唤物来源，不作为技能 VFX 首选 |
| `Assets/FantasyWorkshop/` | `equipment` `ui` | 武器、饰品、符文、装备 sprites | 适合装备/图标/武器参考，不作为 VFX 首选 |
| `Assets/Feel/` | `tool-plugin` | 反馈系统、震动、镜头/屏幕反馈 demo | 可用于反馈思路，不直接当美术素材包 |
| `Assets/GabrielAguiarProductions/` | `uncertain` | 当前扫描未看到明显资源内容 | 默认跳过，除非用户后续确认内容 |
| `Assets/Game VFX - Sword Trails/` | `slash` `dash-trail` | 剑光、刀光、近战拖尾 | 近战/刺客/剑气高相关候选，最终仍应整理成项目专用 prefab |
| `Assets/Game VFX -Explosion & Crack/` | `burst` `impact` `persistent-area` | 爆炸、冲击、地裂、烟尘 | 爆点和地裂高相关候选；注意尺寸、遮挡和顶视角可读性 |
| `Assets/Gizmos/` | `system` | 编辑器 gizmo 图标 | 当前素材搜索时跳过 |
| `Assets/HeroEditor4D/` | `character` `equipment` | 4D 角色、装备、sprites、角色 prefab | 角色本体高优先级来源；不是技能 VFX 包 |
| `Assets/Hovl Studio/` | `slash` `burst` `impact` | Toon VFX 和 sword slash VFX | 中高优先级候选；先检查是否偏 3D、偏侧视角或 shader 依赖 |
| `Assets/Hun0FX/` | `buff-debuff-loop` `aura-shield` | Buff/Debuff、元素状态、角色周身状态 VFX | 状态特效高优先级来源 |
| `Assets/KriptoFX/` | `slash` `projectile` `impact` | Weapon Effects 2，偏武器和命中表现 | 中优先级；先检查依赖、视角和风格 |
| `Assets/Lana Studio/` | `top-down-friendly` `projectile` `impact` `persistent-area` `aura-shield` | Casual RPG VFX，含顶视角战斗常用特效 | 战斗 VFX 高优先级来源，已有项目 prefab 使用经验 |
| `Assets/Layer Lab/` | `ui` `hud` | 多套 GUI / RPG / Casual UI 包 | UI 候选；战斗技能 VFX 搜索时跳过 |
| `Assets/Piloto Studio/` | `persistent-area` `buff-debuff-loop` `burst` `character` | 元素 VFX、龙卷、毒酸、少量角色/模型 | 中优先级；毒、酸、龙卷可查，但多数需处理竖向体积和视角 |
| `Assets/PixPlays/` | `persistent-area` `aura-shield` `burst` | Elemental AOE、Auras、Beams、Shields、Blast | 元素技能候选；使用前先确认风格、尺寸和 prefab 依赖 |
| `Assets/PlayMaker/` | `tool-plugin` | 可视化脚本工具 | 不作为资源素材来源 |
| `Assets/Plugins/` | `tool-plugin` | 第三方工具和插件集合，含 AllIn1VfxToolkit 等 | 先按子插件单独判断；不要直接把整个目录当素材包 |
| `Assets/Prefabs/` | `project-owned` | 项目自有最终 prefab，含 Heroes 和 VFX | 最终战斗资源优先落在这里，不直接当外部素材库乱取 |
| `Assets/Resources/` | `project-owned` | 项目运行时加载资源，含 Stage01Demo、HeroPreview、UI | 修改前确认运行时加载路径，不作为外部素材包 |
| `Assets/Scenes/` | `scene-data` `project-owned` | 项目场景 | 与素材搜索无关，修改需按场景规则验证 |
| `Assets/Scripts/` | `project-owned` | 项目 C# 脚本 | 不是素材目录 |
| `Assets/SF Studio/` | `projectile` `impact` | SF Projectiles Vol 1/2，飞行物、发射、命中 | 飞行物和命中效果高优先级来源 |
| `Assets/SpecialSkillsEffectsPack/` | `burst` `persistent-area` `script-based` | 大型技能、黑洞、龙卷、轨道打击、地裂等 | 大招级候选；默认只当原料，警惕脚本依赖、体积过大和顶视角不适 |
| `Assets/SPUM/` | `character` `ui` | 像素/纸片角色、UI、样例 | 可作角色/UI来源；战斗 VFX 低优先级 |
| `Assets/SrRubfish_VFX_03/` | `projectile` `impact` `persistent-area` | 火系 projectile、impact、firewall、power area 等 | 火系 VFX 高相关；许多 prefab 需重组成项目专用版本 |
| `Assets/Standard Assets/` | `system` `environment` | Unity 旧 Standard Assets、ImageEffects、Utility | 当前阶段低优先级，除非需要旧环境/投影辅助 |
| `Assets/Super Pixel Effects Pack 1/` | `aura-shield` `dash-trail` `impact` `buff-debuff-loop` | 用户确认：光环、气泡、冲锋、展开式能量护盾、落地气流 | 高优先级；像素风，稳定使用前重组为项目专用 prefab |
| `Assets/Super Pixel Effects Pack 2/` | `burst` `impact` `persistent-area` `buff-debuff-loop` | 用户确认：气泡、圆形范围、十字、星星、火柱 | 高优先级；火柱需检查遮挡和竖向感 |
| `Assets/Super Pixel Effects Pack 3/` | `burst` `impact` `dash-trail` | PNG / spritesheet 为主，没有现成 prefab | 中优先级图包；需要自己组装 Sprite 动画或 prefab |
| `Assets/Super Pixel Explosion FX Pack 1/` | `burst` `impact` | 用户确认：爆炸动作和 PNG 图包，prefab 没什么用 | 中优先级；按自组装素材库处理 |
| `Assets/Super Pixel Projectiles Pack 3/` | `projectile` | 用户确认：prefab 全是飞行物 | 飞行物高优先级；像素风需确认 |
| `Assets/Super Pixel Projectiles Pack 4/` | `projectile` | 用户确认：prefab 全是飞行物 | 飞行物高优先级；部分可能偏科幻 |
| `Assets/Synty/` | `ui` `hud` | InterfaceCore 和 FantasyWarriorHUD | UI/HUD 候选；战斗技能 VFX 搜索时跳过 |
| `Assets/TextMesh Pro/` | `system` `ui` | 字体、TMP shader、sprite 资源 | UI 字体系统，不作为 VFX 素材来源 |
| `Assets/UltimateCleanGUIPack/` | `ui` | Clean GUI Pack，主题、按钮、面板等 | UI 候选；战斗技能 VFX 搜索时跳过 |
| `Assets/Vefects/` | `aura-shield` `buff-debuff-loop` `urp-shader-risk` | Level Up VFX URP，升级/仪式/光柱类效果 | 低中优先级；URP/shader 风险，默认不首选 |
| `Assets/vFavorites/` | `tool-plugin` | 编辑器收藏/辅助目录 | 当前素材搜索时跳过 |
| `Assets/VFXDemoEnvironment/` | `environment` | VFX demo 环境植被 | 仅作演示环境或补景，战斗 VFX 搜索时低优先级 |

## 人工校准总览

| 资源库或目录 | 用户确认内容摘要 | 适合用途 | 慎用点 | 推荐优先级 | 记录日期 |
| --- | --- | --- | --- | --- | --- |
| `Assets/UltimateCleanGUIPack/Themes/Classic/Prefabs/` | 该 `Prefabs` 目录里全是 UI 素材 | `ui` | 不作为战斗 VFX / 技能特效候选 | 低 | 2026-04-25 |
| `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/` | 该 `Prefabs` 目录里全是 UI 素材，倾向 HUD 类 | `ui` `hud` | 不作为战斗 VFX / 技能特效候选；`FullscreenFX` 等目录也按界面资源处理 | 低 | 2026-04-25 |
| `Assets/Super Pixel Projectiles Pack 3/Prefabs/` | 该包的 prefab 全都是飞行物素材 | `projectile` | 像素风较强，最终仍优先整理成项目专用 projectile prefab | 高 | 2026-04-25 |
| `Assets/Super Pixel Projectiles Pack 4/Prefabs/` | 该包的 prefab 全都是飞行物素材 | `projectile` | 像素风较强，最终仍优先整理成项目专用 projectile prefab | 高 | 2026-04-25 |
| `Assets/Super Pixel Explosion FX Pack 1/` | 全是爆炸特效素材，但主要是动作和 PNG 图包，需要自行组装；prefab 没什么用 | `burst` `impact` | 不能按现成 prefab 直接使用，需用 PNG/动画序列组装项目专用爆炸 prefab | 中 | 2026-04-25 |
| `Assets/Super Pixel Effects Pack 2/Prefabs/` | 有气泡类、圆形范围、十字型、星星、火柱类特效 | `burst` `impact` `persistent-area` `buff-debuff-loop` | 像素风较强，火柱类需确认是否遮挡英雄或过于竖向 | 高 | 2026-04-25 |
| `Assets/Super Pixel Effects Pack 1/Prefabs/` | 有光环、气泡、冲锋、展开式能量护盾、落地产生的气流特效 | `aura-shield` `dash-trail` `impact` `buff-debuff-loop` | 像素风较强，稳定使用前仍优先整理成项目专用 prefab | 高 | 2026-04-25 |
| `Assets/Lana Studio/Casual RPG VFX/Prefabs/` | 重点战斗 VFX 包，覆盖顶视角区域、爆发、火焰、雾、法球、投射物/命中、治疗、护盾、斩击、状态、顶视角攻击 | `top-down-friendly` `projectile` `impact` `burst` `persistent-area` `aura-shield` `buff-debuff-loop` `slash` | 资源包 prefab 仍只当原料；`Loot`、`Backlight_resources` 更偏掉落/UI反馈，不作为技能特效首选 | 高 | 2026-04-25 |
| `Assets/SpecialSkillsEffectsPack/AllEffects/` | 重点大招级技能包，含龙卷、黑洞、轨道打击、核爆、地裂、护盾、时空、火焰、冰、毒、光束、空袭等大型效果 | `ultimate-scale` `burst` `persistent-area` `aura-shield` `slash` `projectile` | 很多效果偏演示级、体积大、竖向强；`ScriptBased` 目录需额外警惕脚本依赖，不要直接整套挂进战斗 | 中 | 2026-04-25 |
| `Assets/SF Studio/SF Projectiles Vol 2/Prefabs/` | 重点飞行物包，`Projectiles` 下按 `Launch / Proj / Hit` 三段组织，含箭、原子、闪电、能量球、激光、螺旋等系列 | `projectile` `impact` | 最终仍优先整理成项目专用 projectile prefab；激光/科幻感素材需确认是否适合奇幻角色 | 高 | 2026-04-25 |

## 详细记录

### `Assets/Lana Studio/Casual RPG VFX/Prefabs/`

- 用户确认内容：
  - 用户点名要求重点记录该资源包。
  - 截图显示该包位于 `Lana Studio/Casual RPG VFX/Prefabs/`，包含 `Area_generic`、`Backlight_resources`、`Burst`、`Fire`、`Fog`、`Loot`、`Orbs`、`Range_attack`、`Regeneration`、`Shields`、`Slash`、`States`、`Top_down_attack` 等子目录。
- 适合用途：
  - `Area_generic`：顶视角范围底圈、技能区域提示、持续区域底板。
  - `Burst`：瞬时爆发、命中爆点、击飞/震荡圈、范围技能起手反馈。
  - `Fire`：火系持续区域、火焰簇、火焰命中、火法技能原料。
  - `Fog`：减速、毒雾、寒气、速度变化、范围氛围。
  - `Orbs`：法球、环绕能量、元素聚集、施法蓄力原料。
  - `Range_attack`：远程普攻、技能 projectile、发射段、命中段。
  - `Regeneration`：治疗、恢复区域、生命回复 loop、治疗命中反馈。
  - `Shields`：元素护盾、保护技能、短时屏障反馈。
  - `Slash`：近战斩击、剑气、刺客/战士命中特效。
  - `States`：眩晕、睡眠、加速、减速、升级等角色状态提示。
  - `Top_down_attack`：顶视角攻击表现，优先用于需要明确俯视可读性的技能或普攻。
- 不适合或慎用：
  - 不要直接长期引用资源包原始 prefab；稳定使用前应整理到 `game/Assets/Prefabs/VFX/` 下的项目专用 prefab。
  - `Backlight_resources` 更偏掉落背光、资源拾取反馈，不作为战斗技能特效首选。
  - `Loot` 更偏掉落/拾取流程，不作为第一阶段自动战斗技能 VFX 首选。
  - 火焰、护盾、范围类效果接入前仍要检查排序层级，避免遮挡英雄。
- 代表素材或子目录：
  - `Area_generic`：扫描到 8 个 prefab，例如 `Area_generic_blue`、`Area_generic_green`、`Area_generic_red`、`Area_generic_yellow`。
  - `Burst`：扫描到 12 个 prefab，例如 `Burst_rings`、`Burst_sharp`、`Flash_circle`、`Flash_generic`。
  - `Fire`：扫描到 13 个 prefab，例如 `Fire_cartoon_fire`、`Fire_cartoon_poison`、`Fire_cartoon_frost`。
  - `Range_attack`：扫描到 20 个 prefab，例如 `Hit_fire`、`Hit_electric`、`Hit_frost`、`Hit_light`。
  - `Regeneration`：扫描到 8 个 prefab，例如 `Regeneration_health_area_loop`、`Regeneration_health_loop`。
  - `Shields`：扫描到 5 个 prefab，例如 `Shield_fire`、`Shield_wind`、`Shield_electric`。
  - `Slash`：扫描到 12 个 prefab，例如 `Slash_fire_long`、`Slash_magic_long`、`Hit_magic`。
  - `States`：扫描到 8 个 prefab，例如 `Stun_loop`、`Aura_acceleration`、`Aura_slowdown`。
  - `Top_down_attack`：扫描到 19 个 prefab，例如 `top_down_beam_circle_green`、`top_down_beam_line_blue`。
- AI 补充判断：
  - 这是当前第一阶段最值得优先查的通用战斗 VFX 包之一，尤其适合顶视角自动战斗。
  - 当前火法普攻、小技能和大招已经实际从这个包取过火焰、拖尾或相关 VFX 原料，因此后续火系、状态、治疗、护盾、远程攻击都可以优先从这里找候选。
  - 如果需求是“先快速找一个方向”，优先看 `Top_down_attack`、`Area_generic`、`Range_attack`、`Burst`、`States`；如果需求已经明确为治疗/护盾/火系/斩击，再进入对应专门目录。
- 推荐优先级：
  - `高`
- 记录日期：
  - `2026-04-25`

### `Assets/SF Studio/SF Projectiles Vol 2/Prefabs/`

- 用户确认内容：
  - 用户提供截图要求记录该资源包。
  - 截图显示该包位于 `SF Projectiles Vol 2/Prefabs/`，包含 `Core` 和 `Projectiles` 两个子目录。
- 适合用途：
  - 远程普攻 projectile。
  - 技能 projectile。
  - 飞行物发射瞬间特效。
  - 飞行中弹体和拖尾。
  - 命中段 `Hit` 特效。
  - 弓箭、魔法弹、能量球、闪电弹、激光、旋涡弹等不同远程表现方向。
- 不适合或慎用：
  - 不建议长期直接引用资源包原始 prefab；稳定使用前应整理到 `game/Assets/Prefabs/VFX/Projectiles/` 下的项目专用 prefab。
  - 激光、Atom、部分能量类素材可能偏科幻，给奇幻英雄使用前要确认风格是否匹配。
  - projectile 视觉不能反向改变命中时机、飞行速度或目标追踪逻辑。
- 代表素材或子目录：
  - `Core`：扫描到 `VFX PJV2 Root`。
  - `Projectiles`：扫描到 105 个 prefab，按 `Launch`、`Proj`、`Hit` 三段成套组织。
  - 代表系列：`Arrow Blue/Earth/Fire/Pastel Red/Void`、`Atom Blue/Earth/Fire/Pastel Red/Void`、`Electric Blue/Earth/Fire/Pastel Red/Void`、`Energy Ball Blue/Earth/Fire/Pastel Red/Void`、`Laser Simple`、`Laser Sparks`、`Spirals`。
  - 顶层还包含 `Documentation`、`Materials`、`Models`、`Scenes`、`Textures`、`Upgrades`。
- AI 补充判断：
  - 这是做远程普通攻击和技能飞行物时的高优先级来源，尤其适合需要完整“发射-飞行-命中”三段表现的 projectile。
  - 如果只是找飞行命中点，也可以单独拆 `Hit` 段作为 `castImpactVfxPrefab` 或共享命中特效原料。
  - 接入时优先保持运行时 projectile 逻辑不变，只把视觉 prefab 整理进项目自有 `VFX/Projectiles`。
- 推荐优先级：
  - `高`
- 记录日期：
  - `2026-04-25`

### `Assets/SpecialSkillsEffectsPack/AllEffects/`

- 用户确认内容：
  - 用户点名要求重点记录该资源包。
  - 截图显示重点目录为 `EffectsSet_1(NotScriptBased)/Effects/`，里面有 `Effect_01_StormTornado`、`Effect_02_BlackHole`、`Effect_03_OrbitalStrike`、`Effect_04_ChargeShot`、`Effect_05_Nuke`、`Effect_06_BloodFlood`、`Effect_07_OneHandSmash`、`Effect_08_GroundSlash`、`Effect_09_GuardianShield`、`Effect_10_SpaceFleetCall` 等。
- 适合用途：
  - 大招级爆发：`Nuke`、`RuinExplosion`、`BlastFlame`、`MagmaStrike`、`PlanetCrash`、`PlanetStone`。
  - 大范围压场：`BlackHole`、`TimeField`、`IceField`、`LightningField`、`PoisonSmoke`、`DarkChainSwamp`。
  - 轨道/空袭/炮击：`OrbitalStrike`、`SateliteCannon`、`Airstrike`、`AerialBombing`、`SpaceFleetCall`。
  - 斩击/近战大招：`GroundSlash`、`MassiveSlash`、`CriticalSlash`、`MadnessSlash`、`SwordDance`、`SwordForce`。
  - 护盾/防御大招：`GuardianShield`、`GloryShield`、`GloryBoundary`。
  - 火焰/冰/毒/风/雷元素：`FlameBreath`、`FlameWall`、`IceFatalWheel`、`PoisonExplosion`、`WindCyclone`、`LightningStrike`。
  - 投射/连射/光束：`ChargeShot`、`RapidFire`、`FireBall`、`MultipleShot`、`CoreBeam`、`AnnihilationBeam`。
- 不适合或慎用：
  - 默认不适合直接把资源包原始 prefab 当最终战斗 VFX 使用，必须先检查尺寸、视角、持续时间、遮挡和排序层级。
  - 很多效果是演示级大型 3D 表现，可能竖向体积过强、覆盖英雄或占满战场。
  - `EffectsSet_2(ScriptBased)` 需要额外警惕脚本依赖、演示逻辑和运行时控制方式；除非确实需要，不作为第一选择。
  - 对第一阶段 5v5 顶视角自动战斗来说，这个包更适合“拆出一个大招核心视觉原料”，不适合整套照搬。
- 代表素材或子目录：
  - `EffectsSet_1(NotScriptBased)/Effects/`：扫描到 55 个效果目录，偏非脚本版大型技能 prefab。
  - `EffectsSet_2(ScriptBased)/Effects/`：扫描到 46 个效果目录，偏脚本驱动大型技能。
  - `EffectsScenes/`：演示场景。
  - `Models/`、`Textures/`、`Shader_ForEffects/`：模型、贴图和 shader 资源。
  - `Scripts/ForEffects/`：脚本版效果相关代码。
  - `AllEffects/` 下合计扫描到 349 个 prefab。
- AI 补充判断：
  - 这个包的定位应是“大招素材矿”，不是常规普攻、小技能和状态提示的第一来源。
  - 如果用户要求黑洞、龙卷、轨道打击、核爆、地裂、时空领域、巨大护盾或空袭类大招，可以优先来这里找视觉方向。
  - 落地时应优先新建项目专用 prefab，通常需要只取核心粒子、缩短持续时间、压低高度、降低遮挡，并确认英雄排序在特效上层。
  - 若只是要普通命中、治疗、护盾、状态、投射物，通常先查 `Lana Studio/Casual RPG VFX/`、`SF Studio/` 或 `Super Pixel` 系列，别上来就用这个包。
- 推荐优先级：
  - `中`
- 记录日期：
  - `2026-04-25`

### `Assets/UltimateCleanGUIPack/Themes/Classic/Prefabs/`

- 用户确认内容：
  - 该 `Prefabs` 目录里全是 UI 的素材。
- 适合用途：
  - UI 界面、按钮、面板、窗口或主题相关 prefab。
- 不适合或慎用：
  - 不适合作为战斗 VFX、技能区域、飞行物、命中特效或状态特效候选。
- 代表素材或子目录：
  - 截图显示位于 `UltimateCleanGUIPack/Themes/Classic/Prefabs/`。
- AI 补充判断：
  - 后续搜索战斗特效时默认跳过这个目录；只有做 UI 主题或界面 prefab 时再考虑。
- 推荐优先级：
  - `低`
- 记录日期：
  - `2026-04-25`

### `Assets/Super Pixel Projectiles Pack 3/Prefabs/`

- 用户确认内容：
  - 该包的 `Prefabs` 里全都是飞行物素材。
- 适合用途：
  - 普攻 projectile、技能 projectile、魔法弹、箭矢、飞刀、酸液、飞行命中特效的候选原料。
- 不适合或慎用：
  - 像素风较强，需要确认是否和当前战斗角色、场景风格一致。
  - 不建议直接长期引用资源包原始 prefab；稳定使用前应整理到 `game/Assets/Prefabs/VFX/Projectiles/` 下的项目专用 prefab。
- 代表素材或子目录：
  - `Animations`
  - `PNG`
  - `Prefabs`
  - `Scenes`
- AI 补充判断：
  - 后续找飞行物时应优先检查这个包，尤其适合快速找弹体、拖尾、飞行段和命中点缀的原料。
- 推荐优先级：
  - `高`
- 记录日期：
  - `2026-04-25`

### `Assets/Super Pixel Effects Pack 1/Prefabs/`

- 用户确认内容：
  - 这个 `Prefabs` 目录里有光环、气泡、冲锋的特效、展开式能量护盾、落地产生的气流特效。
- 适合用途：
  - 光环：角色 aura、持续增益、范围提示、状态环绕。
  - 气泡：护盾、治疗、保护、轻量状态提示。
  - 冲锋特效：dash、突进、位移拖尾或冲锋起步/结束反馈。
  - 展开式能量护盾：护盾生成、保护技能、屏障展开瞬间。
  - 落地产生的气流：击退落地、跳跃落地、冲锋收尾、重击落点反馈。
- 不适合或慎用：
  - 像素风较强，需要确认是否和当前战斗角色、场景风格一致。
  - 稳定使用前仍优先整理为 `game/Assets/Prefabs/VFX/` 下的项目专用 prefab。
  - 护盾和光环类如果跟随英雄，应确认不会遮挡英雄主体和血条。
- 代表素材或子目录：
  - `Animations`
  - `PNG`
  - `Prefabs`
- AI 补充判断：
  - 后续做共享冲锋、护盾展开、落地冲击、buff/debuff 光环时可优先检查这个包。
  - 若用于冲锋，优先接入或替换现有共享 dash VFX 路径，不要为单个英雄硬写专属表现。
- 推荐优先级：
  - `高`
- 记录日期：
  - `2026-04-25`

### `Assets/Super Pixel Explosion FX Pack 1/`

- 用户确认内容：
  - 这个文件夹全是爆炸特效。
  - 它主要是动作和 PNG 的图包，需要自己组装。
  - 里面的 prefab 没什么用。
- 适合用途：
  - 爆炸主体、瞬时范围爆发、命中爆点、奥义落点爆炸的原料。
- 不适合或慎用：
  - 不适合直接把资源包 prefab 当最终战斗 VFX 使用。
  - 需要基于 `Animations` 和 `PNG` 重新组装项目专用 prefab 或 Sprite 动画。
  - 像素风较强，需要确认是否和当前战斗角色、场景风格一致。
- 代表素材或子目录：
  - `Animations`
  - `PNG`
  - `Prefabs`
- AI 补充判断：
  - 后续需要爆炸效果时可以优先看这里的图包，但应按“自组装素材库”处理，而不是按“可直接挂载的 prefab 库”处理。
  - 如果用于技能爆发，应整理到 `game/Assets/Prefabs/VFX/Skills/` 或 `game/Assets/Prefabs/VFX/Shared/` 下，再由技能数据引用。
- 推荐优先级：
  - `中`
- 记录日期：
  - `2026-04-25`

### `Assets/Super Pixel Effects Pack 2/Prefabs/`

- 用户确认内容：
  - 这个 `Prefabs` 目录里有气泡类、圆形范围、十字型、星星、火柱类特效。
- 适合用途：
  - 气泡类：护盾、治疗、保护、轻量状态提示。
  - 圆形范围：技能范围提示、瞬时范围爆发、持续地面区域原料。
  - 十字型：命中爆点、圣光/标记类技能、方向性范围提示。
  - 星星：眩晕、祝福、治疗、命中反馈或 UI 化的状态提示。
  - 火柱类：火系爆发、落点爆发、短持续区域特效原料。
- 不适合或慎用：
  - 像素风较强，需要确认是否和当前战斗角色、场景风格一致。
  - 火柱类特效可能偏竖向或遮挡英雄，接入前要先检查顶视角可读性和排序层级。
  - 稳定使用前仍优先整理为 `game/Assets/Prefabs/VFX/` 下的项目专用 prefab。
- 代表素材或子目录：
  - `Animations`
  - `PNG`
  - `Prefabs`
  - `Scenes`
- AI 补充判断：
  - 后续需要范围爆发、状态提示、小型法术反馈时可优先查这个包。
  - 若用于火柱或大范围表现，应先做项目专用 prefab，控制尺寸、持续时间和英雄遮挡。
- 推荐优先级：
  - `高`
- 记录日期：
  - `2026-04-25`

### `Assets/Super Pixel Projectiles Pack 4/Prefabs/`

- 用户确认内容：
  - 该包的 `Prefabs` 里全都是飞行物素材。
- 适合用途：
  - 普攻 projectile、技能 projectile、能量弹、激光、飞行道具或更偏科幻感的飞行物候选原料。
- 不适合或慎用：
  - 像素风较强，需要确认是否和当前战斗角色、场景风格一致。
  - 部分素材可能更偏科幻表达，奇幻英雄使用前要先确认风格匹配。
  - 不建议直接长期引用资源包原始 prefab；稳定使用前应整理到 `game/Assets/Prefabs/VFX/Projectiles/` 下的项目专用 prefab。
- 代表素材或子目录：
  - `Animations`
  - `PNG`
  - `Prefabs`
  - `Scenes`
- AI 补充判断：
  - 后续找飞行物时应优先检查这个包，但需要额外留意科幻感是否过强。
- 推荐优先级：
  - `高`
- 记录日期：
  - `2026-04-25`

### `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/`

- 用户确认内容：
  - 该 `Prefabs` 目录里全是 UI 素材，整体倾向 HUD 类。
- 适合用途：
  - 战斗 HUD、行动条、血条、小地图、指南针、任务提示、输入提示、日志或全屏界面覆盖层。
- 不适合或慎用：
  - 不适合作为战斗 VFX、技能区域、飞行物、命中特效或状态特效候选。
  - 即使目录名包含 `FullscreenFX`、`FullscreenOverlays`，也应按 UI/HUD 覆盖层资源理解，不要误判为技能特效资源。
- 代表素材或子目录：
  - `_CommonComponents`
  - `_PreMadeHUDs`
  - `ActionBar`
  - `Compass`
  - `FullscreenFX`
  - `FullscreenOverlays`
  - `Input_Interactions`
  - `Log`
  - `Map_Compass_Icons`
  - `Minimap`
  - `NPC_HealthBars_EnemyData`
  - `Objectives_Story_Location`
  - `Player_Health_Equipment`
- AI 补充判断：
  - 后续搜索战斗特效时默认跳过这个目录；如果要做第一阶段战斗 HUD 或界面占位，可作为 UI 素材候选。
- 推荐优先级：
  - `低`
- 记录日期：
  - `2026-04-25`
