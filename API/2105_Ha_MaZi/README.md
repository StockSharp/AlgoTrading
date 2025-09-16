# Ha MaZi
[Русский](README_ru.md) | [中文](README_cn.md)

Combines Heikin Ashi candles, an EMA filter, and ZigZag pivot confirmation. A long trade is opened when a bullish Heikin Ashi candle forms at a new ZigZag low above the EMA. Shorts appear on a bearish candle at a new ZigZag high below the EMA. Positions are closed by fixed stop loss or take profit.

## Details
- **Entry Criteria**: ZigZag pivot with Heikin Ashi direction and EMA filter.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss or take profit.
- **Stops**: Fixed stop and target.
- **Default Values**:
  - `MaPeriod` = 40
  - `ZigzagLength` = 13
  - `StopLoss` = 70
  - `TakeProfit` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Heikin Ashi, EMA, ZigZag
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
