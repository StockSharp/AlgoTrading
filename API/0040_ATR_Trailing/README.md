# ATR Trailing Stops

ATR Trailing uses an average true range multiple to trail stops behind open positions. Entries occur when price crosses a moving average, and the trailing stop adjusts with volatility.

As price advances, the stop ratchets up (or down) based on the latest ATR reading, never retreating. This locks in gains as the trend persists.

Exits happen when the trailing stop is triggered or when price crosses back through the moving average.

## Details

- **Entry Criteria**: Price above or below MA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Trailing stop hit or price crosses MA.
- **Stops**: Yes.
- **Default Values**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 3.0m
  - `MAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
