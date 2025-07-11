# OBV Breakout

On-Balance Volume (OBV) tracks buying and selling pressure by accumulating volume. This strategy looks for OBV to break above a high or below a low over the lookback window while price confirms the move.

A breakout in OBV suggests strong interest. The system goes long if OBV exceeds its previous maximum, or short if it breaks the minimum. Crossing the OBV moving average signals an exit.

This combines volume momentum with price action.

## Rules

- **Entry Criteria**: OBV surpasses highest or lowest value in lookback period.
- **Long/Short**: Both directions.
- **Exit Criteria**: OBV crosses its MA or stop.
- **Stops**: Yes.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `OBVMAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: OBV, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
