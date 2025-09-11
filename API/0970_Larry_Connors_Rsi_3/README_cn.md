# Larry Connors RSI 3
[English](README.md) | [Русский](README_ru.md)

基于 Larry Connors RSI 规则的均值回归策略。

当价格位于200期SMA之上且2期RSI从高于触发水平连续三天下降并进入超卖区域时买入。RSI升至超买水平时平仓。

## 细节

- **入场条件**: 收盘价高于SMA且2期RSI从触发水平之上连续三天下跌至超卖。
- **多空方向**: 仅做多。
- **离场条件**: RSI高于超买水平。
- **止损**: 否。
- **默认值**:
  - `RsiPeriod` = 2
  - `SmaPeriod` = 200
  - `DropTrigger` = 60m
  - `OversoldLevel` = 10m
  - `OverboughtLevel` = 70m
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**:
  - Category: Mean Reversion
  - Direction: Long
  - Indicators: RSI, SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
