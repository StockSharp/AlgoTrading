# Adam and Eve Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy combining Heiken Ashi candles with a cascade of simple moving averages. A short is opened when a bearish Heiken Ashi candle without an upper wick appears and all monitored moving averages (5, 7, 9, 10, 12, 14, 20) slope downward. A long position is triggered by a bullish candle without a lower wick and all averages sloping upward. Each trade targets a profit at a distance of one ATR(14) from entry with no stop loss.

## Details

- **Entry Criteria**: previous Heiken Ashi candle without upper (short) or lower (long) wick and aligned SMA stack
- **Long/Short**: Both
- **Exit Criteria**: profit target at ATR(14) distance
- **Stops**: None
- **Default Values**:
  - `AtrPeriod` = 14
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA (5,7,9,10,12,14,20), Heiken Ashi, ATR
  - Stops: Target only
  - Complexity: Intermediate
  - Timeframe: Configurable, default 15-minute
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
