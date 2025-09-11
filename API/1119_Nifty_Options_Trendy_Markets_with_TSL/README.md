# Nifty Options Trendy Markets with TSL Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy using Bollinger Bands with ADX and Supertrend filters. Entries require a volume spike. Positions close on MACD crossovers, ADX weakening or an ATR based trailing stop.

## Details

- **Entry Criteria**:
  - Long: price crosses above upper Bollinger Band && ADX > threshold && volume spike && price above Supertrend
  - Short: price crosses below lower Bollinger Band && ADX > threshold && volume spike && price below Supertrend
- **Long/Short**: Both
- **Exit Criteria**: MACD cross, ADX drop or ATR trailing stop
- **Stops**: ATR trailing stop
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2m
  - `AdxLength` = 14
  - `AdxEntryThreshold` = 25m
  - `AdxExitThreshold` = 20m
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5m
  - `VolumeSpikeMultiplier` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Bollinger Bands, ADX, Supertrend, MACD, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
