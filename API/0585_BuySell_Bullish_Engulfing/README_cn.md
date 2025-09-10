# Buy & Sell Bullish Engulfing 策略
[English](README.md) | [Русский](README_ru.md)

当出现多头吞没形态并满足可选的趋势过滤条件时，该策略开多单。仓位规模按照当前资金的百分比计算，仓位通过止盈或止损自动平仓。

## 细节

- **入场条件**：多头吞没形态，可选 SMA 趋势过滤。
- **方向**：仅做多。
- **出场条件**：止盈或止损。
- **止损**：是，包含止盈和止损。
- **默认值**：
  - `CandleType` = 15 分钟
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
  - `OrderPercent` = 30
  - `TrendMode` = SMA50
- **过滤器**：
  - 类别：形态
  - 方向：多头
  - 指标：K线形态，SMA
  - 止损：是
  - 复杂度：低
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
