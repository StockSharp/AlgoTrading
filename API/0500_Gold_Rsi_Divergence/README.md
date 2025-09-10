# Gold RSI Divergence Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Gold RSI Divergence strategy scalps gold by identifying bullish and bearish divergences between price and the Relative Strength Index (RSI).
When price makes a new low but RSI prints a higher low, the strategy looks to buy.
Conversely, when price makes a new high but RSI prints a lower high, the strategy sells.
Both setups are confirmed only if two pivots occur within a configurable bar range.

## Details

- **Entry Criteria**:
  - **Long**: Price lower low, RSI higher low, RSI < 40.
  - **Short**: Price higher high, RSI lower high, RSI > 60.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Uses stop loss and take profit.
- **Stops**: Fixed stop loss and take profit in pips.
- **Default Values**:
  - `RsiLength` = 60
  - `StopLossPips` = 11
  - `TakeProfitPips` = 33
- **Filters**:
  - Category: Divergence
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
