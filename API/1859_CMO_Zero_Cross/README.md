# CMO Zero Cross Strategy

This strategy trades based on zero line crossings of the Chande Momentum Oscillator (CMO).
When the oscillator crosses below zero, a long position is opened. When it crosses above
zero, a short position is opened. Optional stop loss and take profit levels (in points)
protect the position. Entries and exits for long and short trades can be individually
enabled or disabled.

## Parameters

- `Volume` – order volume.
- `CmoPeriod` – period for the CMO indicator.
- `StopLoss` – stop loss in points.
- `TakeProfit` – take profit in points.
- `AllowLongEntry` – allow opening long positions.
- `AllowShortEntry` – allow opening short positions.
- `AllowLongExit` – allow closing long positions on opposite signal.
- `AllowShortExit` – allow closing short positions on opposite signal.
- `CandleType` – timeframe used for calculations.

## Trading Logic

1. Subscribe to candles of the selected timeframe and calculate the CMO.
2. When CMO crosses from above to below zero:
   - Close short positions if allowed.
   - Open a long position if allowed.
3. When CMO crosses from below to above zero:
   - Close long positions if allowed.
   - Open a short position if allowed.
4. Stop loss and take profit are applied using protective orders in points.

## Notes

- Trading decisions are made only on completed candles.
- The strategy uses StockSharp high-level API and binds indicators through `Bind`.
