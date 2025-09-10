# Vietnamese 3x Supertrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy stacks three SuperTrend indicators with different ATR lengths and multipliers. It scales into long positions when the slow trend is bearish and faster trends show pullback opportunities. An optional break-even stop protects profits once price moves favorably.

## Details

- **Entry Criteria**:
  - Slow SuperTrend in downtrend.
  - **Long 1**: Medium uptrend and fast downtrend.
  - **Long 2**: Medium downtrend and price above fast SuperTrend line.
  - **Long 3**: Fast downtrend and breakout above highest high during the fast downtrend.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - All SuperTrends turn up and the candle closes bearish.
  - Average entry price above current close.
  - Optional break-even stop if enabled.
- **Stops**: Optional break-even stop.
- **Default Values**:
  - `FastAtrLength` = 10
  - `FastMultiplier` = 1
  - `MediumAtrLength` = 11
  - `MediumMultiplier` = 2
  - `SlowAtrLength` = 12
  - `SlowMultiplier` = 3
  - `UseHighestOfTwoRedCandles` = False
  - `UseEntryStopLoss` = True
  - `UseAllDowntrendExit` = True
  - `UseAvgPriceInLoss` = True
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: SuperTrend
  - Stops: Optional
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
