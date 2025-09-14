# Hawaiian Tsunami Surfer
[English](README.md) | [Русский](README_ru.md)

该策略寻找突然的动量尖峰并反向交易。它使用 Momentum 指标计算单个柱体收盘价的百分比变化。当变化超过微小阈值时，视为一次“海啸”。策略在强烈上升后卖出，在强烈下跌后买入。通过 StartProtection 以价格步长设置止损和止盈。

## 细节

- **入场条件**：
  - 当动量百分比 > `TsunamiStrength` 时卖出。
  - 当动量百分比 < `-TsunamiStrength` 时买入。
- **多空方向**：双向。
- **离场条件**：止损或止盈。
- **止损**：是，通过 StartProtection。
- **默认值**：
  - `MomentumPeriod` = 1
  - `TsunamiStrength` = 0.24
  - `TakeProfitPoints` = 500
  - `StopLossPoints` = 700
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 类别：均值回归
  - 方向：双向
  - 指标：Momentum
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：高
