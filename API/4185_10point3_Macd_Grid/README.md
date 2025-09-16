# TenPointThree MACD Grid Strategy

## Overview

This strategy is a C# port of the MetaTrader expert advisor **10p3v003 (10point3.mq4)**. It combines a MACD crossover trigger with a martingale grid engine. The original logic was replicated using StockSharp's high level API with the following key behaviours:

- **MACD signal logic** – A trade direction is determined when the MACD main line crosses the signal line on the shifted bar (`SignalShift`). Long entries require the previous signal value to be below `-TradingRangePips`, the current MACD value to stay below zero, and vice versa for shorts. Signals can optionally be inverted through `ReverseSignal`.
- **Grid layering** – After the first position is opened, additional entries in the same direction are only allowed once price moves against the last fill by at least `GridStepPips`. Every new leg multiplies the volume by `LotMultiplier` (or by `1.5` if `MaxTrades > 12`), mimicking the martingale scaling from MQL4.
- **Risk protection** – The most recent leg is closed and no further entries are added when `OrdersToProtect` or more trades are active and the floating profit exceeds the money threshold. The threshold is based on either the configured risk percent (money management enabled) or on the contract size heuristic (money management disabled).
- **Per-leg exits** – Each leg tracks its own take-profit, virtual stop-loss, and trailing stop. The stop distance matches the original formula: `InitialStopPips + (MaxTrades - existingOrders) * GridStepPips`. Trailing activates only after price moves by `TrailingStopPips + GridStepPips` in favour of the position and closes the leg when price retraces by `TrailingStopPips`.
- **Session filter** – When `UseTimeFilter` is enabled, no new grids are started while the candle time is strictly between `StopHour` and `StartHour`, reproducing the "danger time zone" guard from the script.

All money conversions use the security's `PriceStep`/`StepPrice` metadata. If the exchange does not expose a contract size, a fallback value of `100000` is applied, which matches the original Forex assumption.

## Parameters

| Name | Description |
| ---- | ----------- |
| `CandleType` | Candle subscription used for MACD processing (default: 30 minute timeframe). |
| `Volume` | Base lot size for the first grid order. |
| `TakeProfitPips` | Distance in pips for each leg's take-profit (0 disables). |
| `InitialStopPips` | Base stop distance in pips. The actual stop grows with the number of free grid slots. |
| `TrailingStopPips` | Trailing stop distance in pips applied after the leg is sufficiently profitable (0 disables). |
| `MaxTrades` | Maximum number of simultaneous martingale entries. |
| `LotMultiplier` | Multiplier applied to the volume of each additional grid leg (overridden to `1.5` when `MaxTrades > 12`). |
| `GridStepPips` | Minimum adverse price move (in pips) required before opening the next grid entry. |
| `OrdersToProtect` | Minimum number of active legs before the floating-profit protection can close the latest trade. |
| `UseMoneyManagement` | Enables dynamic lot calculation based on account equity. |
| `AccountType` | Selects the risk formula: `0` – Standard (equity / 10,000); `1` – Normal (equity / 100,000); `2` – Nano (equity / 1,000). |
| `RiskPercent` | Percentage of equity used when money management is enabled. |
| `ReverseSignal` | Inverts long/short MACD signals. |
| `FastEmaLength`, `SlowEmaLength`, `SignalLength` | MACD periods (12/26/9 by default). |
| `SignalShift` | Number of closed bars back used for the crossover check (default: 1). |
| `TradingRangePips` | MACD signal band (in pips) that must be breached before a crossover is accepted. |
| `UseTimeFilter` | Enables the session guard based on `StopHour`/`StartHour`. |
| `StopHour`, `StartHour` | Exclusive range that blocks the creation of a new grid when `UseTimeFilter` is true. |

## Money management notes

When `UseMoneyManagement` is disabled, the base lot (`Volume`) is used directly. Otherwise the EA calculates the lot size from the current equity using the same formulas as the original EA:

- Account type **0**: `Ceil(risk% * equity / 10,000) / 10`
- Account type **1**: `risk% * equity / 100,000`
- Account type **2**: `risk% * equity / 1,000`

Volumes are normalised with `Security.VolumeStep`, then capped by `Security.MinVolume`/`MaxVolume`.

## Execution workflow

1. Subscribe to the configured candle stream and feed the MACD indicator through `BindEx`.
2. On each finished candle, update trailing/stop logic for active legs.
3. When the MACD crossover rules fire, ensure the session filter allows trading, the grid direction matches the existing position, and the price has moved by `GridStepPips` against the last fill.
4. Calculate the next leg volume using the martingale multiplier and send a market order.
5. Monitor floating profit; once the protection threshold is reached, close the newest leg and pause until the next candle.

## Conversion notes

- All comments have been rewritten in English as required.
- High-level StockSharp API (candles + `BindEx`) is used. Direct indicator value access is avoided.
- Floating-profit calculations rely on `PriceStep`/`StepPrice`. For exotic instruments make sure these fields are filled.
- The strategy maintains per-leg state internally to emulate MQL4 order management, because StockSharp aggregates positions by default.
