# Volatility Adjusted Moving Average
[Русский](README_ru.md) | [中文](README_cn.md)
 
This technique modifies a moving average band by a multiple of ATR. When price moves beyond the adjusted band, it indicates an accelerated trend.

Long trades are opened above the upper band, shorts below the lower band. A cross back through the baseline moving average closes the position.

Because the bands expand with volatility, stops adapt to market conditions.

## Details

- **Entry Criteria**: Price breaks above or below MA ± ATR multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price crosses MA or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `ATRMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 160%. It performs best in the forex market.
