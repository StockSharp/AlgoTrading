# Day Trading PAMXA Strategy

## Overview
The **Day Trading PAMXA** strategy reproduces the MetaTrader 5 expert advisor that combines Bill Williams' Awesome Oscillator momentum reversals with a stochastic filter. The StockSharp port keeps the original multi-timeframe design:

- The main decision loop runs on the **Signal Candles** timeframe (default 1 hour).
- The Awesome Oscillator is evaluated on a separate **AO Candles** timeframe (default 1 day) to obtain higher time-frame momentum.
- The stochastic oscillator uses its own **Stochastic Candles** timeframe (default 1 hour) so that %K/%D levels are aligned with the original settings.

The strategy holds at most one position at a time. Whenever a bullish setup appears it first covers any active shorts before entering long, and vice versa for bearish setups.

## Entry logic
1. Compute the most recent finished values of Awesome Oscillator on the AO timeframe.
2. Compute the most recent finished %K and %D values of the stochastic oscillator on the Stochastic timeframe.
3. On every finished Signal candle:
   - **Bullish setup**: Triggered when the previous AO bar was below zero and the latest bar closed above zero (momentum reversal) while either %K or %D is below the `Stochastic Level Down` threshold (oversold condition). Any open short is covered and a new long is opened if no position remains.
   - **Bearish setup**: Triggered when the previous AO bar was above zero and the latest bar closed below zero while either %K or %D is above the `Stochastic Level Up` threshold (overbought condition). Any open long is closed and, if flat, a new short position is opened.

## Exit and risk management
- A pip-based **stop loss** and **take profit** are attached at entry. When the candle's low (for longs) or high (for shorts) breaches the stop level the position is liquidated immediately. The same logic applies to the profit target.
- An optional **trailing stop** activates once price has moved by `Trailing Stop + Trailing Step` pips in favour of the position. For longs the stop follows the highest high minus the trailing distance; for shorts it follows the lowest low plus the trailing distance. The trailing adjustment occurs only when the move exceeds the trailing step, mirroring the original EA behaviour.
- Money management can operate in two modes:
  - `FixedVolume`: uses the `Order Volume` parameter directly.
  - `RiskPercent`: computes volume so that the configured percentage of portfolio value would be lost if the stop loss is hit. The calculation uses the pip-based stop distance and rounds to the nearest volume step.
- The strategy never pyramids – once a position exists the next opposing signal will flatten it before any new entry is considered.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `Stop Loss` | Stop-loss distance in pips. Zero disables the protective stop.
| `Take Profit` | Take-profit distance in pips. Zero disables the profit target.
| `Trailing Stop` | Trailing stop activation distance in pips. Zero disables trailing.
| `Trailing Step` | Additional pips required before the trailing stop advances. Must be positive when trailing is enabled.
| `Money Mode` | Selects between `FixedVolume` and `RiskPercent` sizing.
| `Money Value` | Interpreted as lot size when using fixed volume, or as risk percentage when using risk-based sizing.
| `Order Volume` | Base volume used when `Money Mode` is `FixedVolume`.
| `Stochastic %K` | Lookback length for the stochastic %K calculation.
| `Stochastic %D` | Smoothing length for the stochastic %D line.
| `Stochastic Slow` | Final smoothing factor applied to the stochastic oscillator.
| `Level Up` | Upper stochastic threshold that enables short entries.
| `Level Down` | Lower stochastic threshold that enables long entries.
| `Signal Candles` | Timeframe that drives the main trading loop.
| `Stochastic Candles` | Timeframe feeding the stochastic oscillator.
| `AO Candles` | Timeframe feeding the Awesome Oscillator.
| `AO Fast` / `AO Slow` | Periods for the internal moving averages of the Awesome Oscillator.

## Implementation notes
- Pip value calculation emulates the MetaTrader logic: when the security uses 3 or 5 decimal places a pip equals ten price steps; otherwise it equals one price step.
- The StockSharp stochastic oscillator does not expose a dedicated "price field" selection; the port uses the default close-based calculation while retaining the configurable smoothing parameters.
- Trailing stop handling is implemented as a virtual check on candle highs/lows. This replicates the server-side stop adjustments performed in MetaTrader without registering explicit stop orders.
- The code subscribes to all required candle timeframes through `GetWorkingSecurities`, allowing the engine to request data for the signal, stochastic and AO timeframes concurrently.
- English inline comments document the most important control-flow decisions for easier maintenance.

## Usage tips
- Align the `Signal Candles` timeframe with the timeframe you plan to backtest or trade on. Keep `Stochastic Candles` and `AO Candles` equal to the original defaults when you want to mirror the MQL5 expert exactly.
- When switching to `RiskPercent` sizing ensure the stop-loss distance is non-zero; otherwise the strategy falls back to `Order Volume`.
- The default trailing configuration mirrors the original EA (25 pip trailing stop with 5 pip step). Set `Trailing Stop` to zero if you prefer a static stop-loss.
