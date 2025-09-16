# Lacus Trailing Stop & Breakeven Risk Manager

## Overview
This strategy is a direct port of the MetaTrader expert advisor **LacusTstopandBE.mq5**. It does **not** open trades on its own. Instead, it manages an already opened netted position by:

- attaching stop-loss and take-profit levels (or simulating them in stealth mode),
- moving the stop to breakeven after a configurable profit buffer,
- trailing the stop once the price moves further into profit,
- closing the position once an individual profit target is reached,
- closing the whole position when global account profit targets (absolute or percentage) are achieved.

The strategy uses high-level StockSharp APIs (`SubscribeCandles`) and is designed to run on a single security. All order placement helpers (`SellStop`, `BuyLimit`, etc.) are used only when the stealth mode is disabled.

## Trading Logic
1. **Initial protection**
   - When the position changes from flat to long/short, the strategy calculates the requested stop-loss and take-profit offsets (expressed in pips) and registers corresponding protective orders.
   - When stealth mode is enabled, the target prices are stored internally instead of registering orders.
2. **Breakeven handling**
   - After the price moves by `BreakevenGainPips`, the stop-loss is moved to the entry price plus/minus `BreakevenLockPips` (only if the lock distance is smaller than the trigger distance).
3. **Trailing stop**
   - Once the price has advanced by `TrailingStartPips`, the stop follows the price by `TrailingStopPips`. The trail only moves in the direction of profit.
4. **Stealth execution**
   - In stealth mode the stop/take levels are monitored on each finished candle and the position is closed manually when touched.
5. **Profit targets**
   - `PositionProfitTarget` closes the active position when the mark-to-market profit exceeds the specified currency amount.
   - `ProfitAmount` closes the position when the total strategy PnL (realized + unrealized) exceeds the amount.
   - `PercentOfBalance` closes the position when the current portfolio value grows by the configured percentage relative to the portfolio value captured at strategy start.

## Parameters
| Name | Description | Default | Notes |
|------|-------------|---------|-------|
| `StopLossPips` | Stop-loss distance expressed in pips. | 40 | Multiplied by `Security.PriceStep`. |
| `TakeProfitPips` | Take-profit distance in pips. | 200 | Multiplied by `Security.PriceStep`. |
| `TrailingStartPips` | Gain (in pips) required to activate the trailing stop. | 30 | Set to zero to disable trailing. |
| `TrailingStopPips` | Distance maintained by the trailing stop. | 20 | Works together with `TrailingStartPips`. |
| `BreakevenGainPips` | Profit (in pips) that triggers a move to breakeven. | 25 | Must be greater than `BreakevenLockPips`. |
| `BreakevenLockPips` | Pips locked after breakeven is triggered. | 10 | Set to zero to move the stop exactly to the entry price. |
| `PositionProfitTarget` | Currency profit that closes the active position. | 4 | Uses mark-to-market PnL. |
| `ProfitAmount` | Currency profit that closes the strategy position. | 12 | Based on `Strategy.PnL`. |
| `PercentOfBalance` | Percentage of the initial portfolio value that triggers closing. | 1 | Captured from `Portfolio.CurrentValue`. |
| `UseStealthStops` | Simulates stop-loss/take-profit without sending orders. | `false` | Stops/targets are checked on candle close. |
| `CandleType` | Candle series used for price monitoring. | 1-minute time frame | Adjust to match the desired update frequency. |

## Important Notes
- The strategy assumes a **netted** account. If the broker supports hedging positions per order, adaptions are required.
- When running in stealth mode, exits are evaluated only on finished candles. Use a sufficiently small time frame for timely reaction.
- Global profit targets rely on `Portfolio.CurrentValue`. Make sure the connected adapter provides this information.
- Commission and swap adjustments from the original MQL script are not available; the mark-to-market PnL is used instead.
- To mirror the MQL behaviour, ensure the strategy volume matches the currently opened position volume before starting the strategy.

## Conversion Details
- MQL functions `SetSLTP`, `Movebreakeven`, `TrailingStop`, `CloseOnProfit`, `CloseAll`, and `CloseonStealthSLTP` are mapped to separate helper methods in C#.
- Point-to-price conversion uses `Security.PriceStep`, matching the `SymbolInfo.Point()` logic in MetaTrader.
- The expert advisor's `AccountInfo` profit checks are reproduced with `Strategy.PnL` and portfolio value deltas.
- Magic number handling is not required in StockSharp, so it was omitted.
- All comments were rewritten in English as requested.
