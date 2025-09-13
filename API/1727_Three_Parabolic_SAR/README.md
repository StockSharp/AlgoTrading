# Three Parabolic SAR
[Русский](README_ru.md) | [中文](README_cn.md)

The Three Parabolic SAR Strategy uses three Parabolic SAR indicators computed on 6‑hour, 3‑hour and 1‑hour candles. A trade is opened on the 1‑hour timeframe when the higher two timeframes confirm the direction and the 1‑hour SAR flips.

## Details

- **Entry Criteria**:
  - SAR on 6h candle is below close and SAR on 3h candle is below close for long; above for short.
  - On 1h candles the SAR crosses price: from above to below for long, from below to above for short.
- **Long/Short**: Both directions.
- **Exit Criteria**: Position is closed when the 1h SAR moves against the position or when either higher timeframe SAR reverses.
- **Stops**: No.
- **Default Values**:
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `HigherTimeframe` = TimeSpan.FromHours(6)
  - `MiddleTimeframe` = TimeSpan.FromHours(3)
  - `TradingTimeframe` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Parabolic SAR
  - Stops: No
  - Complexity: Basic
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
