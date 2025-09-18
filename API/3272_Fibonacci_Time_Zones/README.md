# Fibonacci Time Zones Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader expert advisor "Fibonacci Time Zones". It keeps the discretionary flavor of the original script by combining a higher timeframe MACD filter with Bollinger band exits and a rich money management module. All trade management routines were rewritten using the high level API: the strategy subscribes to two candle streams (a trading timeframe and a slower timeframe for MACD confirmation) and binds indicators directly through `Bind`/`BindEx` callbacks.

## Core Logic

1. **Momentum filter** – A monthly (configurable) MACD histogram is calculated. A bullish cross above the signal line schedules long entries, while a bearish cross schedules short entries. The actual position is opened on the next trading candle to avoid repeated orders on the same cross.
2. **Entry execution** – Each signal sends a user defined number of market orders. Existing opposite exposure is flattened before opening a new position.
3. **Exit rules** – Multiple layers of defence are applied:
   - **Bollinger band exit**: longs are closed when price touches the upper band, shorts when the lower band is hit.
   - **Classic stop/target**: static stop-loss, take-profit and trailing-stop distances are converted from pips to price units and passed into `StartProtection`.
   - **Break-even**: after price travels a configurable number of pips the stop is pulled to break-even plus an offset. If price retreats to that level the position is closed.
   - **Money trailing**: open and realised PnL are monitored. When the floating profit reaches a threshold the strategy starts trailing it and closes everything after a configurable drawdown.
   - **Equity targets**: optional absolute or percentage profit targets close all trades immediately when met.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `UseTakeProfitMoney`, `TakeProfitMoney` | Close all positions when the combined profit (realised + unrealised) reaches the specified amount of account currency. |
| `UseTakeProfitPercent`, `TakeProfitPercent` | Similar to the previous option but measured as a percentage of the starting equity. |
| `EnableTrailingProfit`, `TrailingTakeProfitMoney`, `TrailingStopLossMoney` | Activates money based trailing once the first threshold is reached and protects accumulated gains. |
| `UseStop`, `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Classic stop, target and trailing distances expressed in pips. |
| `UseMoveToBreakEven`, `WhenToMoveToBreakEven`, `PipsToMoveStopLoss` | Controls the break-even behaviour. |
| `NumberOfTrades` | Number of market orders sent for each signal (mimics the original EA which could stack entries). |
| `CandleType`, `MacdCandleType` | Timeframes for the management candles and the MACD filter. |

## Differences From the Original EA

* Chart button handling and graphical Fibonacci objects are not reproduced; the StockSharp port focuses purely on systematic execution.
* The original expert traded on manual button clicks. The port automatically enters on MACD crosses to deliver a deterministic backtestable strategy.
* MetaTrader specific account functions were replaced with StockSharp equivalents (`Portfolio` values and `PnL`).

## Usage Tips

1. Select appropriate candle types before starting the strategy. The defaults correspond to a 15-minute trading chart with a monthly MACD filter.
2. Tune the pip based distances according to the instrument's tick size. The strategy internally converts pips to price using `Security.PriceStep`.
3. For discretionary intervention disable the automatic profit targets and use the Bollinger exit only.
