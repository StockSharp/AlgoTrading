# Close vs Previous Open Strategy

This strategy compares the close of the last finished candle with the open of the preceding candle.
It enters a long position when the latest close is above the previous open and a short position when the latest close is below the previous open.

## Entry Rules
- **Long**: Close of the most recent completed candle is higher than the open of the candle before it.
- **Short**: Close of the most recent completed candle is lower than the open of the candle before it.

## Risk Management
- Optional stop loss and take profit measured in points.
- Optional trailing of the stop loss.

## Parameters
- `Volume` – order volume.
- `UseStopLoss` – enable stop loss.
- `StopLoss` – stop loss distance in points.
- `UseTakeProfit` – enable take profit.
- `TakeProfit` – take profit distance in points.
- `UseTrailingStop` – trail stop loss as price moves.
- `CandleType` – candle series for calculations.

## Notes
- Trades only on fully formed candles.
- Reverses position when the opposite signal appears.
