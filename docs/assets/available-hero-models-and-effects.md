# 可用于英雄模型与战斗特效的现有资源清单

最后更新：2026-04-10

## 文档用途

这份文档用于记录当前仓库里已经存在、并且较适合作为第一阶段原型占位资源使用的内容，重点包括：
- 英雄模型
- 英雄预制体
- 动画资源
- 战斗特效
- 场景环境资源

目标是帮助后续快速判断“项目里哪些资源可以直接拿来试”，避免每次都在插件目录里盲找。

## 当前最值得优先查看的目录

### 1. 英雄占位模型首选

路径：

`game/Assets/Emerald AI/Demo/Demo Source/Prefabs/AI`

这里有现成角色预制体，最适合直接拖进场景做战斗原型占位：
- `Chomper (Melee).prefab`
- `Grenadier (Melee).prefab`
- `Grenadier (Ranged).prefab`
- `Spitter (Melee).prefab`
- `Ellen (Wander - Non-Combat).prefab`

适用判断：
- 适合先验证“战斗里有没有角色站位和外观”
- 适合做原型阶段英雄占位
- 但这些预制体本身带有 Emerald AI 的演示用途组件，正式接入时不应直接把整套 AI 行为照搬到项目战斗逻辑里

### 2. 角色原始模型与动画

路径：

`game/Assets/Emerald AI/Demo/Demo Source/Demo Models Source/Characters`

这里能找到更底层的角色资源，例如：
- `Grenadier\Models\Grenadier.fbx`
- `Spitter\Models\Spitter.fbx`
- `Ellen\Models\Ellen.fbx`

以及配套动画片段，例如：
- `@GrenadierIdle.fbx`
- `@GrenadierRangeAttack.fbx`
- `@GrenadierMeleeAttack.fbx`
- `@GrenadierHit.fbx`
- `@GrenadierDeath.fbx`
- `@ChomperAttack.fbx`
- `@ChomperIdle.fbx`
- `@ChomperRunForward.fbx`
- `@EllenIdle.fbx`
- `@EllenGunShoot.fbx`
- `@EllenDeath.fbx`

适用判断：
- 如果不想直接用 Emerald AI 的整套 prefab，而是只想拿模型和动画自己组装，这里比 `Prefabs/AI` 更合适
- 更适合后续做“模型、动画、战斗逻辑分离”

### 3. 另一套可用的人形角色资源

路径：

`game/Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack`

当前能确认的关键内容：
- `Prefabs/Character/RPG-Character.prefab`
- 大量动作动画，分布在 `Animations/` 下

可直接关注的动画方向包括：
- `Shield`
- `Staff`
- `Swimming`
- `Climb-Ladder`

其中和战斗原型更相关的是：
- `RPG-Character@Shield-Attack1.fbx`
- `RPG-Character@Shield-Block.fbx`
- `RPG-Character@Shield-Idle.fbx`
- `RPG-Character@Staff-Attack1.fbx`
- `RPG-Character@Staff-Cast-Attack1.fbx`
- `RPG-Character@Staff-Cast-L-AOE1.fbx`
- `RPG-Character@Staff-Cast-L-Buff1.fbx`
- `RPG-Character@Staff-Death1.fbx`
- `RPG-Character@Staff-Idle.fbx`
- `RPG-Character@Staff-Revive1.fbx`

适用判断：
- 这套更像“通用 RPG 人形角色 + 动作包”
- 如果想做更贴近职业模板的占位角色，这套比怪物型的 `Chomper`、`Spitter` 更容易转成战士/法师/辅助等

## 战斗特效资源

### 1. Emerald AI 示例技能特效

路径：

`game/Assets/Emerald AI/Demo/Demo Source/Example Abilities/Effect Prefabs`

当前可直接看到：
- `Grenadier Shoot Effect.prefab`
- `Grenadier Projectile.prefab`
- `Grenadier Impact Effect.prefab`

适用判断：
- 很适合先做远程攻击、飞行物、命中特效的占位
- 比从零搭粒子更快
- 适合作为“普攻投射物”或“小技能飞弹”的第一版占位

### 2. Fantasy-Environment 通用粒子

路径：

`game/Assets/Fantasy-Environment/Particles`

当前可直接看到的粒子 prefab：
- `Eyes01ParticleSys.prefab`
- `Fire01ParticleSys.prefab`
- `Fire02ParticleSys.prefab`
- `Firefly01ParticleSys.prefab`
- `Fog01ParticleSys.prefab`
- `Raylight01ParticleSys.prefab`
- `Raylight02ParticleSys.prefab`
- `Smoke01ParticleSys.prefab`

并包含配套材质与贴图：
- `ParticleMat`
- `ParticleTex`

适用判断：
- 适合做环境氛围、地面火焰、烟雾、光束类占位效果
- 不一定是“现成技能特效”，但很适合改成战斗技能表现

## 其他可复用辅助资源

### Emerald AI Resources

路径：

`game/Assets/Emerald AI/Resources`

这里不是英雄模型主来源，但有一些可参考或可复用的运行时资源：
- `AI Health Bar Canvas.prefab`
- `Combat Text.prefab`
- `Combat Text System.prefab`
- `Combat Text Canvas.prefab`
- `Camera Shake System.prefab`
- `Damage Over Time Component.prefab`

以及基础动画：
- `Attack 1.anim`
- `Attack 2.anim`
- `Attack 3.anim`
- `Idle 1.anim`
- `Idle 2.anim`
- `Idle 3.anim`
- `Run.anim`
- `Walk.anim`
- `Hit 1.anim`
- `Dead.anim`

适用判断：
- 更偏辅助演示系统，不是主角模型来源
- 但其中的 UI、受击、飘字、相机震动思路可以当参考

## 环境资源

### Fantasy-Environment

路径：

`game/Assets/Fantasy-Environment/FBX`

这套资源主要是环境和道具，不是英雄模型来源。

能确认的内容包括：
- 建筑：`Barn01.fbx`、`Blacksmith01.fbx`、`House01.fbx`、`Inn01.fbx`、`Windmill01.fbx`
- 道具：`Barrel01.fbx`、`Crate01.fbx`、`Chest01.fbx`、`Table01.fbx`
- 地图装饰：`Rock01.fbx`、`Tree01.fbx`、`Bush01.fbx`、`Plant01.fbx`

对应 prefab 主要在：

`game/Assets/Fantasy-Environment/Prefabs`

适用判断：
- 适合搭竞技场周边装饰、背景、地图边缘环境
- 不适合直接拿来做英雄

## 当前不值得优先看的目录

以下目录目前看起来不太像已经整理好的可用角色/特效库：
- `game/Assets/Art`
- `game/Assets/Prefabs`

这两个目录当前基本没有形成可直接复用的资源内容。

## 快速使用建议

如果当前只是为了尽快把第一阶段原型“看起来像一场战斗”，优先顺序建议如下：

1. 英雄占位先看 `Emerald AI/Demo/Demo Source/Prefabs/AI`
2. 如果想要更像标准人形职业，占位改看 `ExplosiveLLC/RPG Character Mecanim Animation Pack`
3. 远程攻击或命中特效先看 `Emerald AI/Demo/Demo Source/Example Abilities/Effect Prefabs`
4. 地面火焰、烟雾、光束等补充特效看 `Fantasy-Environment/Particles`
5. 地图环境和场景装饰看 `Fantasy-Environment/FBX` 与 `Fantasy-Environment/Prefabs`

## 在 Unity 里怎么快速找

建议先在 Project 搜索框里直接搜：

- `t:Prefab Grenadier`
- `t:Prefab Chomper`
- `t:Prefab Ellen`
- `t:Prefab RPG-Character`
- `t:Model Grenadier`
- `t:Model Ellen`
- `t:Prefab Projectile`
- `t:Prefab Impact`
- `t:Prefab Fire`
- `t:Prefab Smoke`

最简单的确认方式：
- 想看模型外观，就把 `prefab` 直接拖进场景
- 想看原始模型，就点击 `fbx`
- 想看能不能直接当技能特效用，就优先检查 `Effect Prefabs` 和 `Particles` 下的 `prefab`

## 当前结论

当前项目里，真正最像“可以直接拿来作为第一阶段原型资源”的来源主要是：

- `Emerald AI`：角色 prefab、角色模型、基础攻击/命中特效
- `ExplosiveLLC/RPG Character Mecanim Animation Pack`：通用人形角色和大量动作动画
- `Fantasy-Environment`：环境资源与通用粒子特效

如果只是先把战斗原型跑起来，这三块已经够做第一版占位。
