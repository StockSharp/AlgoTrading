# HTH Trader Hedge Strategy

## Overview

This strategy is a direct conversion of the MetaTrader "HTH Trader" expert advisor. It trades a four-leg forex basket and attempts to capture daily mean-reversion between EURUSD and a mirrored basket of USDCHF, GBPUSD, and AUDUSD. The StockSharp port keeps the original risk controls and timing rules while using the high-level API for multi-security trading.

Key characteristics:

- Opens a hedged basket once per day between 00:05 and 00:12 terminal time.
- Uses the previous two daily closes of EURUSD to decide the basket direction.
- Manages four instruments simultaneously: EURUSD (primary security), USDCHF, GBPUSD, and AUDUSD.
- Tracks open profit in pips and supports basket-wide profit and loss targets.
- Includes an emergency doubling feature that adds to profitable legs when the basket drawdown breaches a threshold.
- Closes all trades at 23:00 terminal time or when the basket hits configured profit/loss limits.

## Data requirements

- **Intraday candles**: All four symbols must deliver intraday candles for the timeframe configured in `IntradayCandleType` (default 5 minutes). These candles provide the latest price and the session clock.
- **Daily candles**: Each symbol must provide daily candles so the strategy can monitor the latest two completed daily closes.

## Trading logic

1. At the end of each finished intraday candle the strategy checks current open profit:
   - If `AllowEmergencyTrading` is enabled and total open profit ≤ `-EmergencyLossPips`, the strategy doubles every leg that is currently in profit and disables further emergency trades for that day.
   - If `UseProfitTarget` is enabled and total open profit ≥ `ProfitTargetPips`, the basket is closed immediately.
   - If `UseLossLimit` is enabled and total open profit ≤ `-LossLimitPips`, the basket is closed immediately.
2. Once the clock reaches 23:00 the basket is closed regardless of profit.
3. When no positions are open and the clock is inside the 00:05–00:12 window, the strategy checks the latest two completed daily closes of the primary symbol (EURUSD by default):
   - If the day-over-day percentage change is **positive**, the strategy opens: long EURUSD, long USDCHF, short GBPUSD, long AUDUSD.
   - If the change is **negative**, it opens: short EURUSD, short USDCHF, long GBPUSD, short AUDUSD.
   - If the change is zero or any daily close is missing, the strategy skips trading for that day.
4. All positions are closed using market orders via `ClosePosition`.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `TradeEnabled` | Enables or disables order placement. | `true` |
| `ShowProfitInfo` | Logs the basket profit in pips on every update while positions are open. | `true` |
| `UseProfitTarget` | Enables auto-closing when `ProfitTargetPips` is reached. | `false` |
| `UseLossLimit` | Enables auto-closing when `LossLimitPips` is reached. | `false` |
| `AllowEmergencyTrading` | Allows the emergency doubling feature. | `true` |
| `EmergencyLossPips` | Basket drawdown (in pips) that triggers emergency doubling. | `60` |
| `ProfitTargetPips` | Basket profit (in pips) that triggers closing when `UseProfitTarget` is enabled. | `80` |
| `LossLimitPips` | Basket loss (in pips) that triggers closing when `UseLossLimit` is enabled. | `40` |
| `TradingVolume` | Order volume for each leg. | `0.01` |
| `Symbol2` | Second security (USDCHF by default). | `null` |
| `Symbol3` | Third security (GBPUSD by default). | `null` |
| `Symbol4` | Fourth security (AUDUSD by default). | `null` |
| `IntradayCandleType` | Intraday timeframe used for scheduling and price updates. | `5` minute candles |

## Usage notes

- Assign the primary security (`Strategy.Security`) to EURUSD (or the desired leading pair) and map `Symbol2`, `Symbol3`, `Symbol4` to the correlated instruments before starting.
- Make sure each security has a valid `PriceStep`, otherwise profit calculations in pips cannot be performed and emergency logic will remain idle.
- The emergency doubling feature only adds to legs that are currently profitable; losing legs are left untouched to avoid amplifying drawdown.
- The implementation assumes market orders fill close to the latest candle close. For precise accounting, connect the strategy to a data feed that delivers timely intraday candles.
- Because the logic is driven by a single bar per minute (or chosen timeframe), the original tick-by-tick MQL behaviour may differ slightly in execution timing, but trade sequencing and conditions match the reference expert advisor.
