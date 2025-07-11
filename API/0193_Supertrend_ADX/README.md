# Supertrend Adx Strategy

Strategy based on Supertrend indicator and ADX for trend strength
confirmation. Entry criteria: Long: Price > Supertrend && ADX > 25
(uptrend with strong movement) Short: Price < Supertrend && ADX > 25
(downtrend with strong movement) Exit criteria: Long: Price < Supertrend
(price falls below Supertrend) Short: Price > Supertrend (price rises
above Supertrend)

Supertrend provides a volatility-adjusted path while ADX confirms the power of the move. Trades take place when both indicators line up.

For those aiming to ride strong trends with trailing stops. ATR determines stop placement.

## Details

- **Entry Criteria**:
  - Long: `Close > Supertrend && ADX > AdxThreshold`
  - Short: `Close < Supertrend && ADX > AdxThreshold`
- **Long/Short**: Both
- **Exit Criteria**: Supertrend reversal
- **Stops**: Uses Supertrend as trailing stop
- **Default Values**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Supertrend, ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
