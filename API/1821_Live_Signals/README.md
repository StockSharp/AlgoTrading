# Live Signals Strategy

This strategy replicates the original MetaTrader script `LiveSignals.mq4`.
It reads a list of pre-scheduled trades from a CSV file and executes them at
specified times. The file is loaded once on startup and each record defines
open time, close time, stop-loss and take-profit prices and trade direction.

## File format

Each line in `signals.csv` must contain the following comma-separated fields:

```
number,open_date,close_date,open_price,close_price,take_profit,stop_loss,type,symbol
```

Dates are parsed using invariant culture. The `type` field accepts `Buy` or `Sell`.

## Parameters

- `Volume` – order volume used for each trade.
- `CandleType` – timeframe of candles used for time checks (default 1 minute).
- `FilePath` – path to the CSV file with signals.

## Trading logic

1. Load all signals from the CSV file at start.
2. On each finished candle:
   - If the next signal's open time is reached, open a market position in the specified direction.
   - If a position exists and price hits stop-loss, take-profit or the close time has arrived, exit the position.

The strategy only trades signals defined in the file and does not generate new ones.
