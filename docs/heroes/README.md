# heroes

这个目录用于记录每个英雄的具体设定，不承载阶段规划或系统规则。

目录约定：

- `docs/heroes/warrior/`：战士
- `docs/heroes/mage/`：法师
- `docs/heroes/assassin/`：刺客
- `docs/heroes/tank/`：坦克
- `docs/heroes/support/`：辅助
- `docs/heroes/marksman/`：射手

建议每个英雄独立一个 Markdown 文件，推荐命名：

- `docs/heroes/<class>/<english-name>.md`

建议至少包含以下字段：

- 英文名
- 中文名
- 英雄 ID
- 英雄分类
- 核心定位
- 普攻类型
- 小技能
- 大招
- 设计边界

技能描述建议统一写清：

- 技能类型
- 释放目标
- 造成的效果
- 检测条件
- 当前关键参数（如施法距离、范围、持续时间、CD）
