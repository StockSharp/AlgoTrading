# Exp Amstell Strategy

## Overview
The **Exp Amstell Strategy** is a grid trading system converted from the original MetaTrader 4 expert advisor `exp_Amstell.mq4`. It continuously places buy and sell market orders whenever price travels a configurable number of points away from the most recent fill. Every individual trade is managed independently: once the market moves by the specified take-profit distance, the strategy sends an offsetting order to capture the profit for that single layer.

Unlike momentum-driven systems, Exp Amstell remains active at all times. It does not wait for indicator confirmations and instead accumulates positions on both sides of the book as the market oscillates. This behaviour makes it highly sensitive to the chosen point distances and to the size of each order.

## Trading Logic
- **Tick-based processing.** The strategy subscribes to level1 quotes and reacts to every change in best bid and best ask, just like the `start()` function in the original MQL code.
- **Independent long and short stacks.** Buy orders are allowed when no long trades are open or when the ask price has dropped by at least the re-entry distance from the latest long entry. Sell orders use the symmetrical condition on the bid price.
- **Per-trade take profit.** Each open layer is tracked separately. When the bid (for longs) or ask (for shorts) advances by the configured take-profit points, the strategy closes only that layer with a market order. Other layers remain untouched.
- **FIFO emulation.** Executed trades are recorded in FIFO order to reproduce the ticket-based accounting that MetaTrader applies to hedged positions. This guarantees that partial fills reduce the oldest outstanding layer first.
- **Netted portfolio awareness.** StockSharp maintains net positions. If a new buy order offsets an open short layer, the strategy removes that short from its synthetic stack before recording the remainder as a new long position.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `0.1` | Volume of every market order that opens a new grid layer. |
| `TakeProfitPoints` | `int` | `30` | Distance in MetaTrader points that must be covered by price before an individual layer is closed. |
| `ReentryDistancePoints` | `int` | `10` | Minimal point distance from the latest entry before adding another order on the same side. |

The strategy automatically converts points into actual price steps using the instrument’s `PriceStep`. Five-digit and three-digit quotes receive the MetaTrader-specific multiplier so that `1 point` equals `0.0001` (or `0.01` for JPY-style symbols).

## Implementation Notes
- Level1 data is sufficient; no candle subscription is required. The strategy declares this by overriding `GetWorkingSecurities()` and requesting `(Security, DataType.Level1)`.
- `StartProtection()` is invoked during `OnStarted` to guarantee that the runner closes any leftover position if the strategy stops unexpectedly.
- All comments inside the C# file remain in English, matching the project guidelines.
- Because StockSharp uses netted positions, the port cannot keep opposing buys and sells open simultaneously. When both sides trade at the same time the newer order will flatten the existing exposure before creating a fresh layer.

## Usage Tips
1. **Calibrate the point distances.** Smaller distances create denser grids that can overtrade in volatile markets. Larger distances reduce activity but increase drawdown per layer.
2. **Size orders prudently.** Grid systems accumulate exposure quickly. Test conservative volumes in the Designer/Backtester before switching to live trading.
3. **Consider manual risk controls.** The original expert has no global stop-loss. Combine the strategy with portfolio-level protections to cap tail risk.
4. **Monitor execution quality.** The algorithm assumes that market orders fill near the best bid/ask. Slippage directly affects achieved take-profit distances.

## Source
Converted from `MQL/9027/exp_Amstell.mq4`.
