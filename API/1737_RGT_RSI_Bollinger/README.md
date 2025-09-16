# RGT RSI Bollinger Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines the Relative Strength Index (RSI) with Bollinger Bands to spot mean-reversion opportunities. A long position is opened when RSI indicates an oversold market and price trades below the lower Bollinger Band. A short position is entered when RSI shows an overbought market and price rises above the upper band. The strategy applies an initial stop-loss and later trails the stop once a minimum profit is reached.

The trailing stop locks in profits by following price at a fixed distance once the trade moves favorably. Positions are closed when the trailing stop is hit.

## Details

- **Entry Criteria**: RSI below `RsiLow` and price under lower band for longs; RSI above `RsiHigh` and price above upper band for shorts.
- **Long/Short**: Both directions.
- **Exit Criteria**: Trailing stop hit.
- **Stops**: Initial stop-loss and trailing stop.
- **Default Values**:
  - `RsiPeriod` = 8
  - `RsiHigh` = 90
  - `RsiLow` = 10
  - `StopLossPips` = 70
  - `TrailingStopPips` = 35
  - `MinProfitPips` = 30
  - `Volume` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: RSI, Bollinger Bands
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
