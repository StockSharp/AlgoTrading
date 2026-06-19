# BTC Chop Reversal 策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格触及ATR通道并出现动量转折时交易BTC短期反转。结合EMA、ATR、RSI、MACD直方图以及成交量尖峰过滤器。

## 详情

- **入场条件**：
  - **多头**：`Low < EMA - ATR*Mult` 且 `RSI < Oversold` 且 `MACD hist rising` 且 `Close > Open` 且无卖出量尖峰。
  - **空头**：`High > EMA + ATR*Mult` 且 `RSI > Overbought` 且 `MACD hist falling` 且 `Close < Open`。
- **多空方向**：双向。
- **出场条件**：
  - 使用止盈和止损保护仓位。
- **止损**：止盈0.75%，止损0.4%。
- **默认值**：
  - `EMA Period` = 23。
  - `ATR Length` = 55。
  - `ATR Multiplier` = 4.4。
  - `RSI Length` = 9。
  - `RSI Overbought` = 68。
  - `RSI Oversold` = 28。
  - `MACD Fast` = 14。
  - `MACD Slow` = 44。
  - `MACD Signal` = 3。
  - `Volume MA Length` = 16。
  - `Sell Spike Multiplier` = 1.5。
  - `Take Profit (%)` = 0.75。
  - `Stop Loss (%)` = 0.4。
- **过滤器**：
  - 分类：反转。
  - 方向：双向。
  - 指标：EMA、ATR、RSI、MACD、成交量。
  - 止损：是。
  - 复杂度：中等。
  - 时间框架：短期。
  - 季节性：否。
  - 神经网络：否。
  - 背离：否。
  - 风险等级：中等。
