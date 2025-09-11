# Lorenzo SuperScalp 策略
[English](README.md) | [Русский](README_ru.md)

该剥头皮策略结合RSI、布林带和MACD。当RSI低于45、价格接近下轨且MACD向上穿越信号线时买入；当RSI高于55、价格接近上轨且MACD向下穿越信号线时卖出。设置最小K线间隔以避免过于频繁的交易。

## 细节

- **入场条件**：
  - **多头**：`RSI < 45` 且 `Close < LowerBand * 1.02` 且 `MACD` 向上穿越信号线。
  - **空头**：`RSI > 55` 且 `Close > UpperBand * 0.98` 且 `MACD` 向下穿越信号线。
- **多/空**：双向。
- **退出条件**：相反信号。
- **止损**：无。
- **默认值**：
  - `RSI Length` = 14
  - `Bollinger Length` = 20
  - `Bollinger Multiplier` = 2
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Min Bars` = 15
- **筛选**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：多个
  - 止损：无
  - 复杂度：中等
  - 周期：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
