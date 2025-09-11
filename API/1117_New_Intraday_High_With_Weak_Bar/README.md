# New Intraday High With Weak Bar Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Enters long on a new `HighestLength`-bar high when the candle closes near its low. Exits when price closes above the prior bar's high.

## Details

- **Entry Criteria**:
  - No position, high equals highest high of last `HighestLength` bars and `(close - low)/(high - low) < WeakRatio`.
- **Long/Short**: Long only.
- **Exit Criteria**: Close above previous bar's high.
- **Stops**: No.
- **Default Values**:
  - `HighestLength` = 10
  - `WeakRatio` = 0.15
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: Highest
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
