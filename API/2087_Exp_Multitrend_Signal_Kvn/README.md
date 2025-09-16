# Exp Multitrend Signal KVN Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements the MultiTrend Signal KVN concept. It builds an adaptive price channel using the Average Directional Index (ADX) to determine the lookback window. When price closes above the channel, the strategy opens a long position. When price closes below the channel, it opens a short position.

The channel width is defined by parameter **K** as a percentage of the swing between recent highs and lows. **KPeriod** sets the base number of bars used for calculations, while the ADX value scales the actual window. **KStop** multiplies the average range and is added to breakout trades to determine stop distance.

The strategy is designed for both long and short trading and uses the 4-hour timeframe by default. No explicit stop-loss or take-profit is provided; protection can be enabled through the platform.

## Details

- **Entry Criteria**:
  - **Long**: Close price breaks above the upper adaptive band.
  - **Short**: Close price breaks below the lower adaptive band.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Reverse signal in the opposite direction.
- **Stops**: Optional via strategy protection.
- **Default Values**:
  - `K` = 48
  - `KStop` = 0.5
  - `KPeriod` = 150
  - `AdxPeriod` = 14
  - `Candle Type` = 4-hour candles
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: ADX, SMA, Max/Min
  - Stops: Optional
  - Complexity: Medium
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
