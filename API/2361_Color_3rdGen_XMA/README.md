# Color 3rd Generation XMA Strategy

This strategy trades based on the direction of a third-generation moving average. The indicator is a combination of two exponential moving averages and turns blue when rising and pink when falling. A buy signal is recorded when the average turns upward, and a sell signal is recorded when it turns downward.

Orders are placed only at a user-specified time after a signal appears. Positions can also be closed when the opposite signal is detected or when a predefined holding period expires. Optional stop-loss and take-profit levels are measured in points.

## Parameters

- **Length** – smoothing period of the third-generation average.
- **StartHour** – hour when new positions may be opened.
- **StartMinute** – minute within the hour when openings are allowed.
- **HoldMinutes** – maximum time to keep an open position.
- **Volume** – order volume used for entries.
- **StopLoss** – stop-loss distance in points. `0` disables the stop.
- **TakeProfit** – take-profit distance in points. `0` disables the target.
- **UseLongEntries** – enable long entries.
- **UseShortEntries** – enable short entries.
- **CloseLongBySignal** – close long positions when a sell signal appears.
- **CloseShortBySignal** – close short positions when a buy signal appears.
- **CandleType** – timeframe of candles used for calculations.

## Logic

1. Subscribe to candles of the selected timeframe.
2. Compute the third-generation moving average for each candle.
3. Detect when the average rises or falls between consecutive candles.
4. Store a buy or sell signal based on the direction change.
5. At the specified opening time, enter in the direction of the stored signal.
6. Close positions on opposite signals, when the holding time elapses, or when stop-loss/take-profit levels are reached.
