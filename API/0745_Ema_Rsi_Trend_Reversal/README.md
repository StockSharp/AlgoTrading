# EMA RSI Trend Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that enters long on EMA crossover with RSI confirmation and exits when the opposite crossover occurs with RSI below the level. Uses percent-based take profit and stop loss.

## Details

- **Entry Criteria**:
  - Long: `FastEMA crosses above SlowEMA && RSI > RsiLevel`
- **Long/Short**: Long only
- **Stops**: Percent take profit and stop loss
- **Default Values**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `RsiLength` = 14
  - `RsiLevel` = 50m
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: EMA, RSI
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
