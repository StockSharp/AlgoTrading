# Gaps Strategy

## Overview

The **Gaps Strategy** is a direct port of the MetaTrader 4 expert advisor `gaps.mq4`. The system monitors 15-minute candles and loo
ks for opening gaps that occur outside the previous candle's high/low range. When such a gap appears, the strategy immediately e
nters the market in the direction of the expected mean reversion move.

The StockSharp version follows the original logic while relying on the high-level candle subscription API. All trade management is
done with market orders and no fixed protective orders are placed, mirroring the behaviour found in the MQL code.

## Trading Rules

1. Subscribe to 15-minute candles (configurable through the `CandleType` parameter).
2. Keep the previous completed candle's high and low.
3. When a new candle starts:
   - Calculate the gap buffer: `(MinGapSize + spreadInSteps) * pointValue`.
   - If the open price is **above** `previousHigh + gapBuffer`, open a **short** position.
   - If the open price is **below** `previousLow - gapBuffer`, open a **long** position.
4. Only one trade per candle is allowed. Once an order is placed, the strategy waits for the next candle before generating a new s
ignal.

The spread component uses the current best bid/ask if available. When no quote data is provided, the strategy falls back to a sin
gle price step as a conservative buffer.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `MinGapSize` | `1` | Minimum gap size in price steps that must be exceeded before sending an order. |
| `GapVolume` | `0.1` | Order volume for market entries triggered by gaps. |
| `CandleType` | `15m TimeFrame` | Candle type used for calculations (defaults to 15-minute candles). |

All parameters are registered as `StrategyParam<T>` and support optimisation inside StockSharp Designer or other tools.

## Implementation Notes

- Uses `SubscribeCandles` with `Bind` to process finished candles only.
- Remembers the previous candle's range to avoid recalculating data series.
- Blocks duplicate orders on the same candle by tracking the open time of the bar that triggered the trade.
- Chart output draws the subscribed candles and the strategy trades for quick visual inspection.

## Differences from the MQL Version

- Take-profit and stop-loss levels were not set correctly in the original EA (the MQL code passed values to the wrong parameters)
. The StockSharp port therefore keeps the behaviour of running without protective orders.
- Spread handling now checks real-time bid/ask quotes when available, providing a more adaptive buffer.

## Requirements

- StockSharp API with access to candle data for the selected instrument.
- Level1 quotes are optional but improve spread detection.
