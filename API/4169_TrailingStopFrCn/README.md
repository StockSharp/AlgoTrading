# TrailingStopFrCn Strategy

## Overview

`TrailingStopFrCnStrategy` is a StockSharp port of the MetaTrader expert advisor **TrailingStopFrCn.mq4**. The original script manages stop-loss levels for existing positions using a mix of fixed trailing distances, Bill Williams fractals, or recent candle highs/lows. This port keeps the same flexibility while integrating with the high-level StockSharp API: the strategy subscribes to candles and Level 1 quotes, monitors the current net position, and automatically updates a protective stop order.

Unlike an entry strategy, TrailingStopFrCn focuses solely on risk management. It does not open new positions. Instead, it tracks the existing position of `Strategy.Security`, cancels obsolete stop orders when the position flips, and sends a single aggregated stop order that follows the logic of the MetaTrader advisor.

## Trailing logic

1. **Fixed trailing distance** – when `TrailingStopPips` is greater than zero, the strategy behaves like the original MQL parameter `TrailingStop`. For long positions the stop is placed at `bestBid - distance`, for short positions at `bestAsk + distance`, with `distance = TrailingStopPips × pip size`.
2. **Fractal trailing** – when `TrailingStopPips = 0` and `TrailingMode = Fractals`, the strategy detects five-bar Bill Williams fractals. Each finished candle is added to an internal buffer and, once enough history is available, the candle two bars back is evaluated as a potential fractal. The most recent fractal that is at least `MinStopDistancePips` away from the current price becomes the new stop candidate.
3. **Candle trailing** – when `TrailingStopPips = 0` and `TrailingMode = Candles`, the strategy scans up to the last 99 closed candles and selects the first low (for longs) or high (for shorts) that is separated from the current price by at least `MinStopDistancePips`.

After computing the candidate level the strategy enforces the same protection rules as the MQL version:

- **OnlyProfit** prevents moving the stop unless the new level would lock in profit (stop above entry for longs, stop below entry for shorts).
- **OnlyWithoutLoss** stops further trailing once the active stop-loss already protects the position from losses (in the original script the trailing process stops after breakeven is reached).
- The stop is only moved in the favourable direction: upwards for long positions and downwards for short positions.

Because StockSharp tracks a single net position per security, the stop order volume equals `Math.Abs(Position)` and all underlying fills are aggregated.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `OnlyProfit` | Move the stop-loss only when the new level secures profit relative to the average entry price. Mirrors the `OnlyProfit` flag from MQL. |
| `OnlyWithoutLoss` | Stop trailing once the active stop-loss is at or beyond the entry price. This replicates `OnlyWithoutLoss` from the original advisor. |
| `TrailingStopPips` | Fixed trailing distance expressed in pips. Set to zero to activate fractal or candle trailing. |
| `MinStopDistancePips` | Minimal distance (in pips) between market price and stop-loss. Use it to emulate the broker `MODE_STOPLEVEL` restriction. |
| `TrailingMode` | Chooses the trailing source when `TrailingStopPips = 0`. Options: `Fractals` (Bill Williams five-bar fractals) or `Candles` (recent lows/highs). |
| `CandleType` | Candle data type used to build fractals or to search for swing points. The default is one-hour time frame. |

## Behavioural notes

- The strategy subscribes to Level 1 data to access best bid/ask prices. Fixed-distance trailing reacts immediately to Level 1 updates, while fractal/candle trailing updates when new candles arrive.
- When the position direction changes the current stop order is cancelled before the new order is submitted.
- If no stop candidate is available (e.g., not enough candles), the strategy keeps the existing stop.
- If the broker does not enforce a minimum stop distance you can leave `MinStopDistancePips` at zero.

## Differences from the MetaTrader version

- StockSharp maintains a net position, so individual MetaTrader “tickets” are not tracked. The stop order covers the entire aggregated position.
- The `Magic` filter is not required: the strategy already operates on its own security context.
- Trailing updates are driven by finished candles plus Level 1 data instead of a one-second polling loop.
- Visual chart objects from the original EA are not recreated; instead, you can use StockSharp’s charting helpers when running the sample UI.

## Usage tips

1. Run the strategy together with any entry logic that opens positions on the same `Security`. TrailingStopFrCn will automatically attach a stop order once the position appears.
2. Adjust `CandleType` to match the timeframe that should be analysed for fractals or swing points. Higher timeframes smooth trailing levels, while lower timeframes react faster.
3. Calibrate `MinStopDistancePips` according to your broker’s stop-level limitations. Setting it too low may lead to rejected orders.
4. When testing on historical data ensure that candle subscription and Level 1 messages are available in the data source so that the trailing logic can trigger correctly.
