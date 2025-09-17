# Maximum Trade Volume Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 5 script **Maximum Trade Volume**. The original tool created a panel that displayed the largest lot size that could be opened for market and pending orders in both directions. The StockSharp version reproduces the same risk-control calculation inside the high-level strategy API. Instead of drawing a UI, it updates four public properties and prints informative log messages every time the values change.

The strategy does not send any orders. Its only task is to monitor the portfolio free funds and express them as a valid volume for market buy, market sell, pending buy and pending sell requests. Other automated components can reuse the computed numbers when preparing real orders.

## Parameters
- **Candle Type** – data source that triggers the recalculation cycle. The MQL script reacted to timer events, while this port refreshes the metrics after every completed candle of the chosen type.

## Logic
1. Subscribe to the configured candle series and wait for finished candles.
2. For each update:
   - Collect portfolio information (current balance, current value or starting value as a fallback) to approximate the free margin.
   - Request the margin requirement per unit for buy or sell. If the exchange provides `MarginBuy`/`MarginSell`, those values are used. Otherwise the strategy derives an estimate from `StepPrice`, `PriceStep` and the current close price.
   - Convert available funds into volume, snap the result to `VolumeStep` and apply `VolumeMin`/`VolumeMax` restrictions.
3. Store the normalized volume for market buy, market sell, pending buy and pending sell requests. When a value changes, write an informational log entry (or a warning if the result equals zero).

## Implementation Notes
- Only finished candles are processed to remain consistent with the high-level API requirements.
- `StartProtection()` is invoked during startup once, matching the repository conventions for defensive behaviour.
- Tab indentation and English inline comments follow the global instructions from `AGENTS.md`.
- Pending orders reserve the same margin as market positions in the source script, therefore both calculations reuse the same helper method.

## Usage
Attach the strategy to a security and portfolio. After it starts receiving data you will see log messages such as:

```
Max lot for buy: 1.25
Max lot for pending sell: insufficient free funds
```

The latest results are also exposed via the `MaxMarketBuyVolume`, `MaxMarketSellVolume`, `MaxPendingBuyVolume` and `MaxPendingSellVolume` properties. Use them to feed position sizing rules or display them inside a custom user interface.
