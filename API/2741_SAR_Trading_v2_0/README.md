# SAR Trading v2.0 Strategy

The **SAR Trading v2.0 Strategy** re-creates the classic Cronex expert advisor inside the StockSharp high-level API. It combines a simple moving average (SMA) with the Parabolic SAR in order to time entries and then manages the position with fixed protective orders and a pip-based trailing stop.

- Indicators: Simple Moving Average, Parabolic SAR.
- Default timeframe: 15-minute candles (configurable through `CandleType`).
- Market: Any instrument that provides a meaningful `PriceStep` (pip) value.

## Trading Logic
- The strategy only evaluates entries when no position is open.
- **Long setup:** either the Parabolic SAR value drops below the SMA or the close price from `MaShift` bars ago is below the SMA. This mirrors the MQL rule `SAR < MA || Close[shift] < MA`.
- **Short setup:** either the Parabolic SAR value rises above the SMA or the close from `MaShift` bars ago is above the SMA.
- After submitting an exit order the algorithm waits until the position is flat before considering new signals, matching the single-position behaviour of the original EA.

## Risk Management
- `StopLossPips` and `TakeProfitPips` convert pips into absolute price distances using `Security.PriceStep`.
- `TrailingStopPips` keeps the protective stop a fixed pip distance behind price once the trade is in profit.
- `TrailingStepPips` forces an extra pip buffer before moving the trailing stop again, emulating the "trailing step" logic from the MQL code.
- If the market reaches the stop-loss or take-profit levels the position is closed at market.

## Parameters
- `MaPeriod` (default **18**): number of bars used by the SMA.
- `MaShift` (default **2**): how many bars back to read the close price when comparing against the SMA.
- `SarStep` (default **0.02**): Parabolic SAR acceleration factor.
- `SarMaxStep` (default **0.2**): maximum Parabolic SAR acceleration factor.
- `StopLossPips` (default **50**): fixed stop-loss distance in pips.
- `TakeProfitPips` (default **50**): fixed take-profit distance in pips.
- `TrailingStopPips` (default **15**): trailing stop distance in pips.
- `TrailingStepPips` (default **5**): additional pip gain required before the trailing stop moves again.
- `CandleType`: candle subscription used for the calculations.

## Additional Notes
- The strategy maintains an internal history of closes to reproduce the `iClose(shift)` call used in the MQL version.
- It relies solely on finished candles for decisions, ensuring consistency with the original expert advisor.
- Volume is taken from the strategy `Volume` property; by default each signal submits a single-lot market order.

