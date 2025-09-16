# My System Strategy

## Overview
The **My System Strategy** is a StockSharp port of the MetaTrader 4 expert advisor `MySystem.mq4` (directory `MQL/9601`). The original script evaluates the Bulls Power and Bears Power indicators, combines their values into a composite momentum signal, and opens reversal-style positions when momentum flips sign. This C# version reproduces the core decision process, adds explicit risk management state, and exposes every tunable constant through strategy parameters for optimization.

Unlike the MQL implementation, which directly queried `iBullsPower`/`iBearsPower` with different applied prices on each bar, the StockSharp edition feeds both indicators from the configured candle series and tracks the previous composite value internally. The translation keeps the default 15-minute timeframe, the same take-profit/stop-loss distances, and the trailing exit conditions specified in the source code.

## Trading Logic
1. Subscribe to the configured candle stream (15-minute candles by default) and wait for fully finished candles.
2. For every completed candle, retrieve the latest Bulls Power and Bears Power values and compute their average `((bulls + bears) / 2)`.
3. Maintain the previous average in `_previousAveragePower` to mirror the shift-based calls in MQL.
4. Entry rules (only when no position is open):
   - **Short entry** – if the previous average is greater than the current average and the current average remains positive. This matches the MQL condition `pos1pre > pos2cur && pos2cur > 0`.
   - **Long entry** – if the current average turns negative (`pos2cur < 0`) meaning Bears Power dominates.
5. Exit management executes on every candle even before new signals:
   - Evaluate hard take-profit and stop-loss levels that were recorded when the position was opened.
   - Apply the trailing-stop logic from the source EA: for longs, trail out when momentum weakens (`pos1pre > pos2cur`) and price has advanced by the trailing distance; for shorts, trail out when the composite momentum becomes negative and price has moved the requested distance in favor.
6. If an exit signal fires, call `ClosePosition()` to flatten; the strategy then waits for the next candle to evaluate fresh entries.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `TakeProfitPoints` | Distance to the take-profit level expressed in price steps. | `86` | Mirrors the `TakeProfit` input. Set to `0` to disable the profit target. |
| `StopLossPoints` | Distance to the stop-loss level expressed in price steps. | `60` | Mirrors the `StopLoss` input. Set to `0` to disable the protective stop. |
| `TrailingStopPoints` | Distance used by the trailing exit condition (price steps). | `10` | When zero the trailing logic is bypassed. |
| `OrderVolume` | Volume submitted on every new entry. | `8.3` | Matches the `Lots` parameter in the EA. |
| `PowerPeriod` | Period applied to both Bulls Power and Bears Power indicators. | `13` | Replicates the original period. |
| `CandleType` | Candle series that drives the indicator calculations. | `15m` | Change to port the strategy to another timeframe. |

All parameters are declared via `Param()` to support UI binding and optimization sweeps.

## Risk Management
- Protective levels are stored when `OnPositionChanged` detects a fresh long or short exposure. The distances are converted to absolute prices using a pip size helper that approximates MetaTrader's `Point` logic (`PriceStep`, adjusted for 3/5 decimal FX symbols).
- `ClosePosition()` is invoked once a take-profit, stop-loss, or trailing condition is met, ensuring the strategy exits with a single market order and avoids duplicate close requests.
- No hedging or partial closures are performed; the strategy enforces a single position at a time, exactly like the `OrdersTotal() < 1` guard in the MQL script.

## Conversion Notes
- MetaTrader's `PRICE_WEIGHTED` vs `PRICE_CLOSE` arguments were approximated by storing the previous composite value (`pos1pre`) instead of maintaining two indicator instances with different price feeds. This keeps the behavioral intent without duplicating candle transformations.
- The original EA contained several malformed `OrderSelect` calls inside the trailing logic. The port implements the intended effect—closing trades once price travels the trailing distance while the momentum condition is satisfied—in a deterministic way.
- Trailing exits are evaluated against candle highs/lows to emulate intrabar touches because StockSharp processes completed candles by default.
- Order sizing, stop distances, and indicator periods retain the original defaults so existing optimizations can be replayed without adjustments.

## Usage Tips
1. Attach the strategy to a security that exposes `PriceStep` and `Decimals`. If these are missing, the helper falls back to a pip size of `1`.
2. Adjust `OrderVolume`, `TakeProfitPoints`, and `StopLossPoints` to align with the instrument's contract size and tick value.
3. When testing on different timeframes, remember to update `CandleType` and consider re-optimizing the trailing distance, as shorter bars will reach the threshold more frequently.
4. Use StockSharp charts (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) to validate that entries occur when Bulls and Bears Power cross the specified thresholds.

## Files
- `CS/MySystemStrategy.cs` – strategy implementation using StockSharp's high-level API.
- `README.md`, `README_cn.md`, `README_ru.md` – multilingual documentation for the converted expert advisor.
