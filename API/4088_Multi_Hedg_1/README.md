# MultiHedg 1 (Timed Multi-Currency Hedging)

## Overview
- Conversion of the MetaTrader 4 expert advisor **MultiHedg_1** into a StockSharp high-level strategy.
- Focused on synchronized entries across up to ten forex symbols using a single schedule-driven signal.
- Designed for discretionary hedging setups where the operator chooses the trading direction (buy or sell) for the entire basket.

## Trading Logic
1. **Scheduled entry window** – when the current candle time enters the configured `[TradeHour:TradeMinute, TradeHour:TradeMinute + DurationSeconds]` interval the strategy opens a market order for every enabled symbol. If an opposite net position exists, the volume is increased so that the strategy finishes with a long/short exposure equal to the configured lot for that symbol.
2. **Optional timed exit** – if `UseCloseTime` is enabled the same duration is applied to the close window (`CloseHour:CloseMinute`). During that interval every managed symbol is closed with `ClosePosition`.
3. **Equity protection** – `CloseByPercent` compares the current portfolio equity (`Portfolio.CurrentValue`) with the latest flat balance snapshot. When equity rises above `PercentProfit`% or falls below `PercentLoss`% the strategy immediately liquidates all symbols in the basket.
4. **Balance tracking** – after every forced liquidation (or whenever all symbols are flat) the balance reference is refreshed so the next trading cycle uses the updated equity as the baseline, similar to the original EA that relied on `AccountBalance()`.

## Parameters
| Group | Parameter | Description |
|-------|-----------|-------------|
| Orders | `Sell` | When `true` all entries are sells, otherwise buys. |
| Schedule | `TradeHour`, `TradeMinute` | Opening time of the entry window in terminal time. |
| Schedule | `DurationSeconds` | Width of the entry/exit window (applied to both trade and close windows). |
| Schedule | `UseCloseTime` | Enables the timed exit window. |
| Schedule | `CloseHour`, `CloseMinute` | Opening time of the exit window. |
| Risk | `CloseByPercent` | Activates the equity-based basket liquidation. |
| Risk | `PercentProfit`, `PercentLoss` | Profit and loss thresholds expressed as percentages of the tracked balance. |
| Data | `CandleType` | Timeframe used to drive the schedule checks. Use 1-minute candles to mimic the MT4 tick loop. |
| Symbols | `UseSymbolN` | Enables the N-th slot (defaults match the EA: first six enabled). |
| Symbols | `SymbolN` | Security assigned to the slot. Configure before running the strategy. |
| Symbols | `SymbolNVolume` | Order volume for the slot (default 0.1 … 1.0 lots). |

## Usage Notes
- Assign actual `Security` objects for every slot that has `UseSymbolN = true`. Empty slots are ignored automatically.
- The strategy does not reproduce the MT4 “magic number”; instead it always closes only the positions belonging to the configured symbols.
- `CandleType` should reflect the broker session time used in the original EA. With multi-connector setups subscribe each instrument to the same timeframe so the schedule is consistent.
- Equity protection relies on portfolio data provided by the connected adapter. Ensure the portfolio supplies `CurrentValue`/`BeginValue` for accurate behaviour.
- Python translation is intentionally omitted per request.
