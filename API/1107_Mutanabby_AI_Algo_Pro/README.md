# Mutanabby AI Algo Pro
[Русский](README_ru.md) | [中文](README_cn.md)

The Mutanabby AI Algo Pro strategy enters long when a bullish engulfing pattern aligns with an RSI reading below a threshold and price decline over a specified number of bars. Exits occur on a bearish engulfing pattern or when the stop loss is hit.

## Details
- **Entry Criteria**: Bullish engulfing, stable candle, RSI below threshold, price below value N bars ago.
- **Long/Short**: Long only.
- **Exit Criteria**: Bearish engulfing or stop loss.
- **Stops**: Optional.
- **Default Values**:
  - `CandleStabilityIndex` = 0.5
  - `RsiIndex` = 50
  - `CandleDeltaLength` = 5
  - `DisableRepeatingSignals` = false
  - `EnableStopLoss` = true
  - `StopLossMethod` = EntryPriceBased
  - `EntryStopLossPercent` = 2.0
  - `LookbackPeriod` = 10
  - `StopLossBufferPercent` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
