# Volume Climax Reversal Strategy

Volume Climax Reversal seeks turning points marked by extremely high volume after a strong trend.
Such climactic spikes suggest exhaustion as the last buyers or sellers rush in before momentum fades.

The strategy enters against the prior move once a large volume bar closes and price begins to retrace.

A tight percent stop protects the position, and trades exit if volume fails to drop off or price continues in the original direction.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Volume
  - Direction: Both
  - Indicators: Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
