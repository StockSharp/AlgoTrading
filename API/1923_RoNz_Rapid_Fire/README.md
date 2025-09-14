# RoNz Rapid-Fire Strategy

This strategy combines a moving average with the Parabolic SAR indicator to detect rapid trend changes. A long position is opened when the close price rises above the moving average while the Parabolic SAR flips below the price. A short position is opened on the opposite conditions. Positions can optionally be averaged when the trend continues.

## How It Works
- **Entry Long**: Close price > SMA and Parabolic SAR switches below price.
- **Entry Short**: Close price < SMA and Parabolic SAR switches above price.
- **Close**: Either by stop loss/take profit or by opposite signal depending on the selected mode.
- **Averaging**: Adds new positions when the trend persists.
- **Trailing Stop**: Adjusts the stop price as the trade moves in profit.

## Parameters
- `Volume` – trade volume.
- `StopLoss` – stop loss in ticks.
- `TakeProfit` – take profit in ticks.
- `TrailingStop` – trailing stop in ticks.
- `Averaging` – enable averaging positions.
- `MaPeriod` – moving average period.
- `PsarStep` – Parabolic SAR step.
- `PsarMax` – Parabolic SAR maximum value.
- `CloseType` – `SlClose` uses stops only, `TrendClose` closes on opposite trend.
- `CandleType` – candle series for calculations.

## Notes
- Works with any instrument supported by StockSharp.
- Requires historical candles for the selected `CandleType`.
