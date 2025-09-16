# Maximus vX Lite Strategy

This strategy attempts to trade breakouts from short-term consolidation zones. It searches for compact price ranges on the 15‑minute chart and opens trades when the price breaks away from these ranges by a specified distance.

## Strategy Logic

1. For every finished 15‑minute candle the highest high and lowest low of the last 40 candles are calculated.
2. If the distance between these extremes is below the **Range** parameter a consolidation zone is assumed.
3. After the **Delay Open** period passes without new trades, a breakout above the upper boundary plus **Distance** points triggers a long position, while a breakout below the lower boundary minus **Distance** points triggers a short position.
4. A fixed **Stop Loss** and a trailing stop of **Trail** points are applied once a position is opened.
5. Consolidation boundaries are reset after the **Period** hours elapse.

## Parameters

- `DelayOpen` – Hours to wait before opening a new trade.
- `Distance` – Breakout distance from the consolidation boundary in points.
- `Period` – Hours after which consolidation levels are recalculated.
- `Range` – Maximum size of the consolidation zone in points.
- `StopLoss` – Initial stop loss in points.
- `Trail` – Trailing stop distance in points.

## Notes

The strategy uses only the high-level API: candles are received through `SubscribeCandles`, and indicator values are bound using `Bind`. Orders are sent with `BuyMarket` and `SellMarket` methods. Comments in the source code are written in English.
