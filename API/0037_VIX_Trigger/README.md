# VIX Trigger

VIX Trigger reacts to changes in the Volatility Index. A rising VIX signals fear and possible reversals in the underlying instrument. The strategy compares VIX direction with price relative to a moving average.

When VIX increases and price is below the moving average, it buys expecting a recovery. Conversely, rising VIX with price above the average invites a short position.

Positions close when VIX falls or the stop-loss percentage is reached.

## Rules

- **Entry Criteria**: VIX rising while price relative to MA triggers longs or shorts.
- **Long/Short**: Both directions.
- **Exit Criteria**: VIX falls or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Contrarian
  - Direction: Both
  - Indicators: VIX, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
