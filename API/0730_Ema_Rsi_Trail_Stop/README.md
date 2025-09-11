# EMA RSI Trail Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades crossovers of short and medium EMAs filtered by a long EMA. RSI levels exit trades, and a trailing stop with a fixed stop-loss manages risk. Trades can optionally close after a number of bars if profitable.

## Details

- **Entry Criteria**: EMA A crossing EMA B with trend confirmed by EMA C and candle direction.
- **Long/Short**: Both.
- **Exit Criteria**: RSI thresholds, trailing stop, or time-based exit.
- **Stops**: Fixed percent stop that converts to trailing stop after price moves by `TrailOffset`.
- **Default Values**:
  - `EmaALength` = 10
  - `EmaBLength` = 20
  - `EmaCLength` = 100
  - `RsiLength` = 14
  - `ExitLongRsi` = 70
  - `ExitShortRsi` = 30
  - `TrailPoints` = 50
  - `TrailOffset` = 10
  - `FixStopLossPercent` = 5
  - `CloseAfterXBars` = true
  - `XBars` = 24
  - `ShowLong` = true
  - `ShowShort` = false
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, RSI
  - Stops: Trailing
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
