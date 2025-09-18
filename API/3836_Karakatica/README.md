# Karakatica Strategy

## Overview
The Karakatica Strategy is a medium-term trend-following system that was ported from the original MetaTrader 4 expert advisor "Exp_karakatica". The strategy trades **EUR/USD on the M15 timeframe** by default and uses a custom signal engine that emulates the behaviour of the original "iKarakatica" indicator with a moving-average crossover model. The crossover is recalculated on every bar and the signal period is continuously re-optimised in order to follow the most profitable recent regime.

The strategy enters the market with market orders only when no position is currently open. Protective orders (stop-loss and take-profit) are attached automatically through the StockSharp protection subsystem.

## Trading Logic
1. **Signal generation** – The strategy calculates a simple moving average (SMA) of the candle closing prices. A bullish signal appears when the previous candle closed below or at the SMA while the latest finished candle closes above it. A bearish signal is produced when the previous candle closed above or at the SMA and the latest candle closes below it. Signals are always evaluated on the *previous* completed bar to mirror the MT4 implementation that used shift=1 values from the `iKarakatica` indicator.
2. **Position management** –
   - If an opposite signal appears while a position is open, the position is closed immediately with a market order.
   - New trades are allowed only when no position exists and the strategy is not blocked by the optimisation stage. Consecutive trades in the same direction are blocked until the market produces a confirmed opposite signal.
3. **Order sizing** – The position size is derived from the configured `Risk` parameter. The algorithm converts the risk into a desired volume based on the current portfolio value and then aligns it with the instrument volume step, mimicking the lot-calculation method from the original expert advisor.
4. **Trade protection** – Stop-loss and take-profit distances are set in price points. They are translated into absolute prices by multiplying the point value by the instrument price step.

## Adaptive Optimisation
The expert advisor continuously re-optimises the signal period in order to adapt to changing market behaviour:

1. Every `ReoptimizeEvery` bars the strategy launches a historical simulation that covers `OptimizationDepth` previous bars.
2. For each candidate period in the range `[OptimizationStart, OptimizationEnd]` with a step `OptimizationStep`, the backtester simulates a simple moving-average crossover model:
   - The simulator keeps track of an active virtual position and updates its profit whenever the opposite signal is triggered.
   - Separate profit counters are maintained for long and short trades in addition to the combined profit.
3. After scanning all candidates the strategy applies the following rules:
   - If both long and short profits are negative, trading in both directions is disabled until the next optimisation cycle.
   - If the best long and short results are equal, the overall best period is used and both directions remain enabled.
   - Otherwise only the direction with the highest profit stays enabled and the corresponding best period is selected.

The optimisation requires at least `OptimizationDepth + OptimizationEnd + 2` completed candles to start. Until enough history is collected the strategy delays trading.

## Parameters
| Name | Description | Default | Optimisable |
| ---- | ----------- | ------- | ----------- |
| `Risk` | Percentage of portfolio value (per 1000 units) that defines the target order volume. | 0.5 | Yes |
| `StopLossPoints` | Stop-loss distance in price points. | 50 | Yes |
| `TakeProfitPoints` | Take-profit distance in price points. | 150 | Yes |
| `Period` | Active SMA period used for signal generation. Updated automatically by the optimiser. | 70 | Yes |
| `OptimizationDepth` | Number of historical bars used for the in-sample backtest. | 250 | No |
| `ReoptimizeEvery` | Frequency of optimisation runs measured in finished bars. | 50 | No |
| `OptimizationStart` | Minimum period considered during optimisation. | 10 | No |
| `OptimizationStep` | Step between neighbour periods. | 5 | No |
| `OptimizationEnd` | Maximum period considered during optimisation. | 150 | No |
| `CandleType` | Data type of candles (defaults to 15-minute time frame). | M15 time-frame candles | No |

## Usage Notes
- The strategy was designed for EUR/USD on the 15-minute time frame. When porting to a different instrument please review the point value, volume step, and spread assumptions.
- Make sure that the data feed provides best bid/ask quotes. They are used to estimate the trading spread during the optimisation process. When the quotes are not available the algorithm falls back to a single price-step spread.
- Because the optimisation logic requires several hundred historical bars, allow the strategy to preload data before enabling live trading.

## Files
- `CS/KarakaticaStrategy.cs` – StockSharp implementation of the strategy.
- `README.md` – English description (this file).
- `README_ru.md` – Russian description.
- `README_cn.md` – Chinese description.
