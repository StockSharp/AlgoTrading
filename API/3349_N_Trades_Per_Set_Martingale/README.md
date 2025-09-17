# N Trades Per Set Martingale Strategy

## Overview
This strategy is a direct conversion of the MetaTrader expert advisor "N trades per set martingale + Close and reset on equity increase". It keeps the market direction simple—only long trades are taken—but actively manages position sizing through a martingale cascade and an equity-based reset. A new trade is opened immediately after the previous one closes, keeping the strategy constantly engaged in the market.

## Trading Logic
1. **Sequential entries** – the strategy opens a long market order whenever no position is active. Stop-loss and take-profit orders are attached right after the fill.
2. **Win/loss accounting** – after a position is closed the realized price is compared with the entry price. A profitable closure increments the win counter, otherwise the loss counter is incremented. Break-even results are treated as losses, matching the original EA.
3. **Set completion** – the number of trades in the current set is also tracked. When the counter reaches `Trades Per Set`, the cycle is considered complete and one of three outcomes can happen:
   - **All wins** – the volume is recalculated from the current equity using `Equity Divisor` and the cycle counters are reset.
   - **All losses** – the volume is multiplied by `Scale Factor` and the cycle counters are reset.
   - **Mixed results** – if the set contains both wins and losses the counters are simply reset and the current volume is preserved.
4. **Equity reset** – whenever the portfolio equity grows by at least `Equity Increase`, the strategy performs a global reset. All counters are cleared, the base volume is recalculated from equity, and the equity target is moved forward by the same increment.

This behaviour mirrors the original EA where trade blocks were chained through fxDreema logic nodes.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `Trades Per Set` | Number of sequential trades that form one martingale cycle. |
| `Stop Loss (pips)` | Stop-loss distance measured in price steps of the instrument. Set to zero to disable. |
| `Take Profit (pips)` | Take-profit distance measured in price steps. Set to zero to disable. |
| `Scale Factor` | Multiplier applied to the trade volume after a fully losing set. Values below 1 are automatically clamped to 1. |
| `Equity Divisor` | Divides account equity to derive the base lot size after a fully winning set or an equity reset. |
| `Equity Increase` | Amount of equity growth that triggers the global reset. Set to zero to disable the equity-based exit. |

## Money Management
- The volume is aligned to the instrument constraints (`VolumeStep`, `MinVolume`, `MaxVolume`) in the same manner as the original EA.
- When equity data is unavailable the previous volume is reused, falling back to `VolumeStep` if this is the very first trade.
- Stop-loss and take-profit distances are converted to price steps via `PriceStep`. If the instrument does not specify a price step the raw value is rounded to the nearest integer.

## Usage Notes
- The strategy is long-only, just like the MetaTrader script. If the broker supports shorting, disable it manually when running the strategy.
- Because stop and target orders are recreated after every fill, partial fills are handled gracefully—the remaining volume inherits the same protective orders.
- The equity reset is evaluated after every closed position. Make sure the portfolio connection supplies current equity values so the reset threshold can be reached.
