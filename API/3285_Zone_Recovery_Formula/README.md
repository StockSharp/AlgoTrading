# Zone Recovery Formula Strategy

## Overview

The **Zone Recovery Formula Strategy** is a port of the MetaTrader 4 "Zone Recovery Formula" expert advisor. The algorithm follows a moving-average driven trend direction and then applies a zone recovery technique to mitigate adverse price moves. The core idea is to alternate long and short cycles with gradually increasing volume until price action exits the defined recovery zone, locking in profit even after several reversals.

## How It Works

1. **Signal Detection** – The strategy subscribes to timeframe candles (15 minutes by default) and tracks a fast and a slow simple moving average. A bullish crossover starts a long recovery cycle, while a bearish crossover starts a short cycle.
2. **Initial Order** – When a new cycle starts the strategy opens a market position with the base volume multiplier. The take-profit and recovery distances are calculated from pip settings and the instrument tick size.
3. **Zone Recovery** – If price moves against the open position by the configured recovery distance, the strategy reverses the direction and increases the order size using the original formula sequence (up to the maximum number of trades). This creates an alternating net exposure that aims to cover previous losses once price returns to the profit target.
4. **Profit Management** – The algorithm monitors unrealized profit:
   - Monetary and percentage take-profit conditions can close all positions immediately.
   - Optional trailing management captures profits after a predefined gain and protects them with a trailing stop distance.
5. **Cycle Reset** – When profit targets are achieved or trailing protection closes the position, the recovery cycle is reset and the strategy waits for the next moving-average signal.

## Key Parameters

- **Use TP Money / TP Money** – Enable and configure monetary take-profit.
- **Use TP % / TP Percent** – Enable and configure percentage take-profit based on the portfolio balance.
- **Enable Trailing / Trailing TP / Trailing SL** – Activate trailing profit capture and define the activation level together with the protective distance.
- **TP Pips / Zone Pips** – Distances (in pips) that define the take-profit objective and the recovery trigger zone.
- **Base Volume / Max Trades** – Initial order size and the number of recovery steps allowed in a cycle.
- **Fast MA / Slow MA** – Moving averages that generate entry signals.
- **Profit Offset** – Optional adjustment used in the original recovery volume formula.

## Notes

- The strategy uses the high-level StockSharp API with candle subscriptions and indicator binding.
- Hedging positions are emulated by flipping the net position direction and scaling volume, which keeps the logic compatible with StockSharp's net position accounting.
- Trailing and take-profit checks rely on unrealized profit calculated from the current position price. Adjust the monetary values to match the instrument's tick value.
- Always test on a simulated environment before deploying to a live account.

## Files

- `CS/ZoneRecoveryFormulaStrategy.cs` – C# implementation of the strategy.
- `README.md` – This documentation file in English.
- `README_ru.md` – Documentation in Russian.
- `README_cn.md` – Documentation in Chinese.
