# Z-Score 买卖策略
[English](README.md) | [Русский](README_ru.md)

该策略利用Z-score检测价格相对移动平均线的极端偏离。
当Z-score超过阈值时开仓，并通过冷却期避免重复信号。

## 细节

- **入场条件**：
  - 当 z-score 大于 `ZThreshold` 且卖出冷却结束时做空。
  - 当 z-score 小于 -`ZThreshold` 且买入冷却结束时做多。
- **多空方向**：双向。
- **出场条件**：
  - 反向信号。
- **止损**：无。
- **默认值**：
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：均值回归
  - 方向：双向
  - 指标：SMA、StandardDeviation、Z-Score
  - 止损：无
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
