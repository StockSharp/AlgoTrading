# Gap Fill Reversal Strategy

Gap Fill Reversal takes advantage of overnight gaps that quickly retrace during the next session.
When price gaps away from the prior close but immediately moves back to fill that void, it often signals an exhaustion of the initial move.

The strategy enters once the gap is fully closed and looks for a reversal in the opposite direction of the open.
It aims to capture the snap back that occurs as trapped traders exit their positions.

A percent-based stop defines the risk and positions close when momentum fades or the stop is hit.

## Details

- **Entry Criteria**: pattern match
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Gap
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
