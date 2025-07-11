# Midday Reversal Strategy

Midday Reversal seeks turning points that occur around lunchtime when morning trends often exhaust.
Liquidity typically dries up mid-session, leading to reversals as traders square positions.

The system monitors for a shift in momentum near midday and enters in the opposite direction of the morning move.

A percent stop controls risk and exits occur if the reversal fails to develop by the afternoon.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Intraday
  - Direction: Both
  - Indicators: Price Action
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
