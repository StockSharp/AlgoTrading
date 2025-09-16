# eInTradePanel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The eInTradePanel strategy automates the workflow of the original MetaTrader trade panel. It allows the same eight order modes (market, stop, limit and stop-limit in both directions) while automatically computing trigger, entry, stop-loss and take-profit distances from the current spread and a volatility-sensitive ATR estimate. Protective orders are simulated through candle monitoring so the strategy can be used with data vendors that do not support attached SL/TP orders.

## Highlights

- **Order modes** – choose between Buy, Sell, Buy/Sell Stop, Buy/Sell Limit or Buy/Sell Stop-Limit. Stop-limit orders are armed once price reaches the trigger distance and then submit the limit entry.
- **Dynamic distances** – pending levels, triggers, stops and targets are proportional to the larger of the current spread or an ATR-derived synthetic spread (`ATR × AtrFactor`). When ATR is not ready, a configurable base tick distance is used.
- **Volatility adaptation** – ATR length follows the original panel (55) so offsets react to regime changes without extra tuning.
- **Order expiration** – optional cancellation window with minimum lifetime enforcement (default 11 minutes) keeps stale pending orders off the book.
- **Risk management** – every open position is watched on each closed candle; if the high/low pierces the computed stop or target the position is closed at market.
- **Quote awareness** – the strategy subscribes to the order book to obtain best bid/ask prices for more accurate offset calculations, falling back to candle closes when depth is unavailable.

## Parameters

| Name | Description |
| --- | --- |
| `Volume` | Order size used for all entries. |
| `Mode` | Entry mode (market, stop, limit or stop-limit). |
| `Candle Type` | Aggregation used for ATR and candle-based execution checks. |
| `Base Ticks` | Minimum tick distance when ATR data is not available. |
| `Pending Multiplier` | Multiplier applied to the base tick distance for pending order offsets. |
| `Trigger Multiplier` | Additional multiplier for stop-limit trigger distances. |
| `Stop Multiplier` | Multiplier for stop-loss distance (set to 0 to disable). |
| `Take Multiplier` | Multiplier for take-profit distance (set to 0 to disable). |
| `Use ATR` | Enables ATR-based scaling of all distances. |
| `ATR Factor` | Fraction of ATR treated as synthetic spread when scaling. |
| `Expiration` | Minutes until pending orders are cancelled (0 keeps them GTC). |
| `Min Expiration` | Minimum pending lifetime in minutes, mirroring the panel's guardrail. |

## Trading Logic

1. **Data preparation** – the strategy subscribes to the configured candle type and keeps a 55-period ATR updated. Order book snapshots update the last seen bid/ask.
2. **Distance calculation** – every finished candle recomputes base tick distance from ATR and spread, then derives pending, trigger, stop and take-profit prices according to the selected mode.
3. **Order submission** –
   - Market modes execute immediately at the next finished candle while the strategy is flat.
   - Stop and limit modes place the corresponding pending order and optionally cancel it after the expiration window.
   - Stop-limit modes wait until the trigger price is printed by the candle high/low, then submit the limit entry.
4. **Position supervision** – once a position is open the strategy checks completed candles for stop or target breaches and closes the position at market if either level is violated.
5. **State reset** – when the strategy is flat and no order is active it recomputes levels so a new trade can be staged on the next candle.

The approach mirrors the manual panel while remaining compatible with the StockSharp high-level API and asynchronous order flow.
