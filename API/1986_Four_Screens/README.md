# Four Screens Strategy

The Four Screens strategy trades using Heikin-Ashi candles across four timeframes: 5, 15, 30, and 60 minutes.
It goes long when all timeframes show bullish candles and goes short when all show bearish candles.
Stop-loss and take-profit levels are set in points with optional trailing stop.

## How it works
1. Subscribes to candle streams for 5, 15, 30 and 60 minutes.
2. Calculates Heikin-Ashi open and close for each candle.
3. Marks each timeframe as bullish or bearish.
4. Enters long when all are bullish, enters short when all are bearish.
5. Uses `StartProtection` to apply stop-loss, take-profit and optional trailing.

## Parameters
- `CandleType` – base timeframe for 5 minute candles.
- `StopLossPoints` – stop-loss distance in points.
- `TakeProfitPoints` – take-profit distance in points.
- `UseTrailing` – enable trailing stop (true/false).

The trading volume is defined by the strategy `Volume` property.

## Notes
- Works with high-level API using `SubscribeCandles` and `Bind`.
- Processes only finished candles.
- Comments in code are provided in English.
