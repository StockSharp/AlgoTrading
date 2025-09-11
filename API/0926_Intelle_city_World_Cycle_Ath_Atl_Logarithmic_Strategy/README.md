# Intelle city World Cycle Ath Atl Logarithmic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy uses scaled moving averages to mark all-time-high (ATH) and all-time-low (ATL) signals based on the Pi Cycle concept.

The system sells when the scaled ATH long MA crosses below the short MA and buys when the scaled ATL long MA crosses above the short MA.

## Details

- **Entry Criteria**: Scaled ATH long MA crosses below ATH short MA for sell. Scaled ATL long MA crosses above ATL short MA for buy.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `AthLongLength` = 350
  - `AthShortLength` = 111
  - `AtlLongLength` = 471
  - `AtlShortLength` = 150
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
