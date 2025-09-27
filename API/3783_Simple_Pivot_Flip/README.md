# Simple Pivot Flip Strategy

## Overview
This strategy is a high-level C# port of the MetaTrader 4 Expert Advisor stored in `MQL/7610/Simplepivot_www_forex-instruments_info.mq4`. The original program checks the open price of each new candle against the previous candle range and flips between long and short market positions. The StockSharp version keeps the same behaviour by relying exclusively on high-level helpers such as `SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket`, and `ClosePosition`.

The converted logic:

1. Waits for a finished candle to obtain the open, high, and low values.
2. Uses the previous candle range to build a simple pivot at the midpoint.
3. Opens a new long position when the current candle opens in the lower half of the range or gaps above the previous high.
4. Opens a new short position when the current candle opens in the upper half of the range.
5. Always closes the existing position before entering in the opposite direction, replicating the single-ticket behaviour of the MQL version.

No stop-loss or take-profit levels are implemented in the original Expert Advisor, so the position is reversed only when a new candle dictates a different direction.

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `OrderVolume` | 1 | Market order volume used when entering a position. |
| `CandleType` | 1 minute time frame | Candle type requested from the data feed. |

## Trading Logic Details
1. The very first finished candle is stored and used as the reference for the next decision. No order is sent until there is a complete candle to analyse.
2. For every subsequent completed candle:
   - Compute `pivot = (previousHigh + previousLow) / 2`.
   - If `Open < previousHigh` **and** `Open > pivot`, the strategy prepares a short entry.
   - Otherwise it prepares a long entry (this covers opens in the lower half, opens equal to the pivot, and any gaps above the previous high or below the previous low).
3. If the strategy already holds a position in the chosen direction, the signal is ignored to avoid paying the spread twice—mirroring the early return found in the MQL code.
4. If the direction changes, the current position is closed via `ClosePosition()` and a new market order is sent using `OrderVolume`.
5. The previous high/low buffer is updated with the latest completed candle to drive the next decision.

## Risk Management
The converted algorithm does not include stops or profit targets. Position sizing is controlled only by the `OrderVolume` parameter, so risk should be managed externally (for example by adjusting the volume or by combining the strategy with account-level protections).

## Visualisation
When a chart area is available, the strategy plots the requested candles and the executed trades, which helps validate the pivot flips during backtests.
