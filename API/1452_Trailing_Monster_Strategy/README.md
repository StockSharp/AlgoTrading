# Trailing Monster Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining KAMA trend detection with RSI filter and a trailing stop. Positions open when RSI crosses extreme levels in the direction of the KAMA trend. After a delay, a percentage trailing stop protects profits.

## Details
- **Entry Criteria**:
  - **Long**: RSI > `RsiOverbought`, close above SMA, KAMA rising
  - **Short**: RSI < `RsiOversold`, close below SMA, KAMA falling
- **Long/Short**: Both
- **Exit Criteria**:
  - Percentage trailing stop after `DelayBars`
- **Stops**: Trailing stop in percent
- **Default Values**:
  - `KamaLength` = 40
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `SmaLength` = 200
  - `BarsBetweenEntries` = 3
  - `TrailingStopPct` = 12m
  - `DelayBars` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: KAMA, RSI, SMA
  - Stops: Trailing
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
