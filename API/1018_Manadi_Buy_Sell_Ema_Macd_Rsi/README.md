# Manadi Buy Sell EMA MACD RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

EMA crossover strategy with MACD and RSI confirmations. Market entries with fixed percent stop-loss and take-profit.

## Details

- **Entry Criteria**: EMA crossover with MACD agreement and RSI bounds.
- **Long/Short**: Both directions.
- **Exit Criteria**: Percent-based stop-loss or take-profit.
- **Stops**: Percent-based.
- **Default Values**:
  - `FastEmaLength` = 9
  - `SlowEmaLength` = 21
  - `RsiLength` = 14
  - `RsiUpperLong` = 70
  - `RsiLowerLong` = 40
  - `RsiUpperShort` = 60
  - `RsiLowerShort` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `TakeProfitPercent` = 0.03
  - `StopLossPercent` = 0.015
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: EMA, MACD, RSI
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
