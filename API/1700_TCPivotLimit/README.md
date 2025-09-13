# TCPivotLimit Strategy

This strategy trades around classic daily pivot point levels. Pivot points are calculated from the previous day's high, low and close prices. Limit orders are placed at selected support or resistance levels and positions are managed with predefined stop-loss and take-profit levels.

## Parameters
- **Volume** – order volume.
- **Target Variant** – selects which support/resistance levels are used for entry, stop and target:
  1. Entry at S1/R1, stop at S2/R2, target at R1/S1.
  2. Entry at S1/R1, stop at S2/R2, target at R2/S2.
  3. Entry at S2/R2, stop at S3/R3, target at R1/S1.
  4. Entry at S2/R2, stop at S3/R3, target at R2/S2.
  5. Entry at S2/R2, stop at S3/R3, target at R3/S3.
- **Intraday Close** – close any open position at 23:00.
- **Modify Stop Loss** – move stop loss to the first target level after it has been reached.

## Trading Logic
1. At the start of each day the strategy computes pivot, three resistance and three support levels using the previous day's data.
2. When price touches the chosen support or resistance level a limit order is sent in the opposite direction.
3. Position is closed when stop-loss or take-profit level is hit. Optional stop-loss modification can tighten risk after the first target.
4. If *Intraday Close* is enabled, any open position is closed at the end of the trading session.
