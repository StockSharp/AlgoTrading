# Market Slayer Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses a weighted moving average crossover with a higher timeframe SSL trend confirmation. A long position is opened when the short WMA crosses above the long WMA while the trend is bullish; a short position is opened on the opposite conditions. Optional absolute take profit and stop loss can be enabled.

## Details

- **Entry Criteria**:
  - **Long**: short WMA crosses above long WMA and the higher timeframe SSL is bullish.
  - **Short**: short WMA crosses below long WMA and the higher timeframe SSL is bearish.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Trend filter turns opposite.
  - Optional stop loss or take profit when enabled.
- **Stops**: Optional.
- **Default Values**:
  - `ShortLength` = 10.
  - `LongLength` = 20.
  - `ConfirmationTrendValue` = 2.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
  - `TrendCandleType` = TimeSpan.FromMinutes(240).TimeFrame().
  - `TakeProfitEnabled` = false.
  - `TakeProfitValue` = 20.
  - `StopLossEnabled` = false.
  - `StopLossValue` = 50.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: WMA, SSL
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
