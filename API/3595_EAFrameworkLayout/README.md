# EA Framework Layout (Trade Management)

## Overview

This strategy is the StockSharp port of the MQL5 expert advisor "EA框架布局" (EA framework layout). The original system is not a signal generator. Instead, it manages positions that are opened manually or by other tools. The StockSharp version keeps the same philosophy: it supervises the active position, enforces a time-based exit and monitors the alignment of six exponential moving averages to decide when the position must be closed.

The strategy subscribes to a single candle series (H1 by default) and builds six EMA indicators with periods 2, 4, 6, 8, 12 and 16. Whenever a new candle is completed the indicators are updated and the management logic is executed.

## Strategy Logic

1. **Position tracking** – the strategy detects when a new position appears or when an existing position changes direction. Internal counters are reset so the management window starts from the most recent entry.
2. **Direction filter** – the operator can restrict management to long-only or short-only mode. If an open position violates the allowed direction it is closed immediately.
3. **EMA alignment check** – two recently closed candles (shifts 1 and 2) must keep all six EMAs strictly ordered and sloping in the same direction. If the alignment is lost, the position is closed. This reproduces the `MA_Direction` test from the original MQL5 framework.
4. **Time-based exit** – the `AutoCloseAfterXH1` parameter defines the number of completed H1 candles after the entry when the position must be liquidated. Every finished candle increments the counter; when the threshold is reached the position is closed with a market order.
5. **Manual trading** – no new orders are created by the strategy. Users should open and size positions themselves. The `TradeInitLots` parameter is still exposed so that additional logic can be added later (for example, pyramiding), but the current conversion does not perform automatic averaging or reversal.

## Parameters

| Name | Description |
| --- | --- |
| `LotAmplifier` | Multiplier applied to the base volume (kept for compatibility with the original EA, not used in the current logic). |
| `TradeInitLots` | Base position volume. The strategy sets its default `Volume` to this value. |
| `AutoCloseAfterXH1` | Number of completed H1 candles to wait before closing the position automatically. |
| `IsReverse` | Reserved flag from the MQL version. The converted logic keeps it for completeness but does not perform reversals. |
| `DirectionAllow` | Allowed direction for active positions (`Both`, `Up`, `Down`). |
| `UsTimeLeftBound` / `UsTimeRightBound` | Hour boundaries of the US trading window (fractional hours). Currently provided for parity with the original EA. |
| `NonUsTimeLeftBound` / `NonUsTimeRightBound` | Hour boundaries of the auxiliary trading window. |
| `CandleType` | Candle type used for indicator calculation (default: 1 hour). |

## Usage Notes

* The strategy expects to run together with manual trading or another entry engine. It will not send any entry orders on its own.
* Because the time-based exit counts finished candles, the default 1-hour timeframe must be preserved to achieve the same behaviour as the MQL5 code. Changing the candle series alters the meaning of the auto-close parameter.
* The EMA alignment check requires at least two completed candles after the indicator warm-up. Until the history buffer is populated the strategy ignores the alignment rule to avoid premature exits.
* The session-window helpers (`IsInDealTime`) are retained from the source EA for future extensions. They can be combined with user logic to limit when manual entries are allowed.

## Converting from MQL5

The StockSharp port uses the high-level API (`SubscribeCandles`, indicator binding and `BuyMarket`/`SellMarket`) as recommended in the project guidelines. Indicator values are received through the bind callback, which eliminates the need to fetch historical buffers manually. The `MA_Direction` routine from the original base class is reproduced in C# to evaluate EMA alignment without storing large collections.
