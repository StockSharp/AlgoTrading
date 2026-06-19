# RSI 和 ATR 趋势反转 SL TP
[English](README.md) | [Русский](README_ru.md)

该策略利用 RSI 和 ATR 计算动态阈值，通过价格与阈值交叉来捕捉趋势反转，并内置止损和止盈。

## 详情

- **入场条件**：价格突破自适应 RSI/ATR 阈值。
- **多空方向**：双向。
- **出场条件**：反向突破。
- **止损/止盈**：通过动态阈值实现。
- **默认值**：
  - `RsiLength` = 8
  - `RsiMultiplier` = 1.5
  - `Lookback` = 1
  - `MinDifference` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别: Trend
  - 方向: Both
  - 指标: RSI, ATR
  - 止损: 是
  - 复杂度: Intermediate
  - 时间框架: Intraday (5m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险级别: 中等
