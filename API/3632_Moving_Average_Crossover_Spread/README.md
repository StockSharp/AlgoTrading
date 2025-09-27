# Moving Average Crossover Spread Strategy

This strategy is a StockSharp port of the MQL4 expert advisor **"EA - Moving Average"** (file `EA - Moving Average.mq4`).
It trades a single instrument by reacting to moving average crossovers that are detected at the open of every new candle.

## Core Idea

- Use a fast and a slow exponential moving average (EMA) calculated on the selected candle series.
- Wait until a new candle is available and evaluate the EMA values of the two most recent completed candles, replicating the `iMA(..., shift=1/2)` calls from the original code.
- Open a **long position** when the fast EMA has crossed above the slow EMA on the previous candle while the candle before that still had the fast EMA below the slow EMA.
- Open a **short position** when the fast EMA has crossed below the slow EMA on the previous candle while the candle before that still had the fast EMA above the slow EMA.
- Only one position can be open at a time. The strategy ignores new signals until all orders are closed.

## Order Management

- Before placing an order the current spread is checked. If the best ask and best bid are available the spread is converted into instrument points and compared with `MaxSpreadPoints`. Signals that exceed the limit are skipped, just like the original `MarketInfo(..., MODE_SPREAD)` guard.
- After a market order is submitted the strategy mirrors protective levels around the entry price:
  - The stop-loss is placed at the slow EMA value of the previous candle plus/minus the configured `StopLossPoints`.
  - The take-profit is set at the same distance from the entry price as the stop-loss, creating a symmetric target as in the MQL implementation (`Ask + (Ask - StopLoss)` / `Bid - (StopLoss - Bid)`).
- All price distances expressed in points are translated into absolute prices via the instrument `PriceStep`, so the behaviour matches the point-based configuration from MetaTrader.

## Conversion Notes

- The original expert allows choosing different moving-average modes, but its defaults use EMA (`MAMode = 1`). The StockSharp version focuses on EMA to keep the implementation concise; different smoothing algorithms can be added if needed.
- Trade volume is provided through the `TradeVolume` parameter and mapped to `Strategy.Volume` during `OnStarted`.
- The strategy relies purely on candle data supplied through `CandleType`. There are no additional indicator collections or historical buffers besides the two-value EMA history required to detect crossovers.

## Parameters

- `CandleType` – candle data type and timeframe to subscribe to.
- `FastPeriod` – length of the fast EMA (defaults to 21).
- `SlowPeriod` – length of the slow EMA (defaults to 84).
- `StopLossPoints` – stop-loss distance in instrument points relative to the slow EMA.
- `MaxSpreadPoints` – maximum allowed spread in points before a new order is denied.
- `TradeVolume` – lot size used when sending market orders.

## Usage Tips

1. Select the symbol and candle timeframe before starting the strategy so the EMA values match the intended chart in MetaTrader.
2. Provide level1 data (best bid/ask) if you want the spread filter to work in real time; otherwise the strategy assumes the spread is acceptable.
3. Make sure the security has a valid `PriceStep`. Without it the strategy cannot translate point-based distances into absolute prices and will skip protective order placement.
