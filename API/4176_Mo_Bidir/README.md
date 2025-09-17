# MO Bidir Hedge Strategy

## Overview
The **MO Bidir Hedge Strategy** is a StockSharp port of the MetaTrader 4 expert advisor `mo_bidir_v0_1`. The original robot was designed for the five-minute chart and always kept a hedged market exposure: every new bar opened both a long and a short position with pre-defined stop-loss and take-profit distances. The StockSharp version reproduces this behaviour using finished candles, high-level order helpers, and explicit risk parameters measured in instrument points.

## Trading Logic
1. Subscribe to the configured candle type (five-minute timeframe by default) and process only finished candles.
2. As soon as a candle closes, inspect the internal hedge legs. If any leg remains open, the strategy waits for protective orders to trigger and does not open additional positions.
3. When no legs are active, submit a **market buy** and a **market sell** order of equal size. Each executed order becomes an independent hedge leg tracked by the strategy.
4. After each entry is filled the stop-loss and take-profit thresholds are calculated by multiplying the configured point distances by the instrument price step (or minimal price increment when the step is unavailable).
5. On every subsequent finished candle, the strategy checks candle highs and lows:
   - Long legs close via a market sell when the low breaches the stop level; if not stopped, a high that reaches the target closes the leg for a profit.
   - Short legs close via a market buy when the high touches the stop; otherwise, a low that reaches the target realises the profit.
   - When both thresholds fall inside the same candle the stop-loss is prioritised because its touch would have closed the position first in the MetaTrader implementation.
6. Once all legs are closed by their protective levels the strategy immediately prepares the next hedged pair on the following candle close.

This workflow maintains parity with the MT4 logic while relying exclusively on high-level StockSharp APIs (`BuyMarket`/`SellMarket`) and candle-based processing mandated by the conversion guidelines.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Order size applied to both sides of the hedge. Must be positive. |
| `StopLossPoints` | Distance from the entry price to the protective stop measured in instrument points. Use `0` to disable the stop. |
| `TakeProfitPoints` | Target distance from the entry price in instrument points. Use `0` to disable the profit target. |
| `CandleType` | Timeframe used to detect new bars. Defaults to a five-minute time frame. |

All point-based distances are converted to absolute prices by multiplying the configured value by the instrument `PriceStep`. If the step is undefined the minimal price increment is used; when neither value is available the protective levels remain inactive.

## Risk Management
- Both sides of the hedge use the same fixed volume and rely on symmetric protective orders.
- Stop-loss and take-profit distances mirror the MetaTrader defaults (80 and 750 points respectively), preserving the "8 pips vs. 75 pips" relationship on a 5-digit forex symbol.
- Each leg is closed with a market order, instantly releasing margin and allowing the remaining leg to continue unmanaged until its own protective level is hit.

## Implementation Notes
- The strategy strictly processes **finished candles** to comply with the project-wide conversion rules. Intrabar stop or target touches are inferred from the candle extremes, so backtests without tick data will assume the stop triggered before the target when both prices appeared within the same bar.
- The internal hedge ledger keeps track of fills independently from the net portfolio position. This mirrors the MetaTrader behaviour where long and short positions coexist simultaneously.
- No automated trailing logic, session filters, or additional indicators are introduced—the StockSharp version intentionally remains as minimalistic as the original expert advisor.

## Usage Tips
- Adjust `TradeVolume` to match broker contract sizes and ensure the instrument supports simultaneous buy/sell hedging if the environment requires it.
- If you need pip-based values (e.g., 8 pips), multiply by the number of points that represent a pip for the current symbol before assigning the parameter.
- Combine the strategy with StockSharp risk modules or `StartProtection` if extra portfolio-level safeguards are required.
