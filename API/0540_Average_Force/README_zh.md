# Average Force 策略
[English](README.md) | [Русский](README_ru.md)

Average Force 策略使用一个振荡器来衡量收盘价在设定周期内最高价和最低价之间的位置，并通过移动平均线平滑结果。正值表示向上的力量，负值表示向下的压力。

当平滑后的 Average Force 大于零时策略做多，当其小于零时策略做空。

## 详情

- **入场条件**:
  - Average Force > 0 → 买入。
  - Average Force < 0 → 卖出。
- **多空方向**: 多头和空头。
- **出场条件**:
  - 当 Average Force 穿越零轴并向相反方向移动时反转仓位。
- **止损**: 无。
- **默认参数**:
  - `Period` = 18
  - `Smooth` = 6
- **过滤器**:
  - 类别: 动量
  - 方向: 双向
  - 指标: Highest、Lowest、SMA
  - 止损: 无
  - 复杂度: 低
  - 时间框架: 任意
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险水平: 低
