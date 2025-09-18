# L3H3 Pivot Strategy

## Overview

The **L3H3 Pivot Strategy** is a StockSharp port of the MetaTrader expert "L3_H3_Expert". The original script builds a daily pivot structure and deploys two pending orders to trade potential breakouts or pullbacks around the previous session's high and low. The StockSharp version keeps the same idea: it recalculates the pivot levels after each completed higher-timeframe candle (daily by default) and decides between stop or limit entries based on where the market currently trades relative to yesterday's range.

## Trading Logic

1. **Session statistics**
   - After every completed pivot candle (default: daily), the strategy captures the previous session's open, high, low, and close values.
   - The classical pivot level is calculated as `(High + Low + Close) / 3`.
   - These levels remain active for the entire next session.

2. **Entry setup**
   - A buy entry price is anchored slightly above the previous low. The offset equals the `EntryOffsetPips` parameter expressed in pip-size multiples.
   - A sell entry price is anchored at the previous high (mirroring the original expert that used the raw high without any additional buffer).
   - For every new trading day (detected via the main candle subscription), the strategy places fresh pending orders:
     - If the market trades **below** yesterday's low, a **buy-stop** is placed to catch an upside breakout.
     - If the market trades **above** yesterday's high, a **sell-stop** is placed to trade a downside reversal.
     - Otherwise, the algorithm prefers **limit** orders at the same price levels to buy dips or sell rallies back into the range.
   - Stop-loss orders are positioned `StopLossPips` away from the reference low/high, exactly as the MQL version fixed a 16-point stop buffer.
   - The take-profit of both pending orders is aligned with the pivot level, replicating the target placement found in the source code.

3. **Order management**
   - Every time a fresh pivot is calculated, any working pending orders are cancelled and recalculated with the new levels.
   - The strategy also cancels outdated pending orders when a new session begins, preventing accumulation of inactive orders.
   - When an order is filled, its internal reference is cleared automatically to avoid duplicate cancellations.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `EntryCandleType` | Candle series used to monitor the current session and trigger order placement. | 5-minute time frame |
| `PivotCandleType` | Higher timeframe candle used to measure the previous session statistics. | Daily time frame |
| `EntryOffsetPips` | Distance (in pips) added above the previous low for long entries. | 2 |
| `StopLossPips` | Distance (in pips) applied beyond the reference low/high to position stop losses. | 16 |

## Differences from the MQL Expert

- The MetaTrader script selected different trading sessions (Asian, London, New York) via magic numbers and time windows. The StockSharp version consolidates the behaviour by using a configurable higher timeframe candle (daily by default) to derive the pivot levels, which makes the logic easier to audit and adapt across brokers.
- MetaTrader relied on the current bid/ask for deciding between stop and limit orders. The StockSharp implementation uses the most recent finished candle of the `EntryCandleType` series for that comparison to keep the workflow event-driven.
- Order comments and magic numbers were platform-specific in MT4. They are intentionally omitted here; instead, the strategy maintains direct references to its pending orders.

## Usage Notes

- Ensure that the underlying security exposes a valid `PriceStep`. The strategy throws an exception on start if the broker connection does not provide pip size information.
- To replicate the original behaviour more closely, set the `PivotCandleType` to an hourly candle series aggregated over your desired session and adjust the offset/stop parameters accordingly.
- As with any pending-order strategy, consider the broker's minimum distance and pending-order expiration policies when deploying live.

