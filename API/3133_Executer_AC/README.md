# Executer AC Strategy

The **Executer AC Strategy** is a faithful StockSharp port of the MetaTrader 5 "Executer AC" expert advisor. The original EA trades on the **Accelerator Oscillator (AC)** developed by Bill Williams and combines its momentum swings with a fixed stop/limit framework and a trailing stop module. This conversion keeps the behaviour of the MQL5 version while exposing user-friendly parameters that integrate with the high-level StockSharp API.

## Trading logic

The strategy operates on finished candles of the selected timeframe and relies on the last four Accelerator Oscillator values:

- `AC[0]` – most recent completed bar (called `ac[1]` in the original code).
- `AC[1]`, `AC[2]`, `AC[3]` – progressively older values used for pattern detection.

The decision tree is identical to the EA:

1. **Position management**
   - Long positions are closed when `AC[0] < AC[1]` (momentum decreasing).
   - Short positions are closed when `AC[0] > AC[1]` (momentum increasing).
   - A trailing stop routine dynamically tightens the protective stop once price moves beyond the configured distance plus the trailing step.
2. **Entry rules when flat**
   - **Bullish acceleration above zero:** if `AC[0] > 0` and `AC[1] > 0` and `AC[0] > AC[1] > AC[2]`, a market buy is issued.
   - **Bearish acceleration above zero:** if `AC[0] > 0` and `AC[1] > 0` and `AC[0] < AC[1] < AC[2] < AC[3]`, a market sell is issued.
   - **Bullish acceleration below zero:** if `AC[0] < 0` and `AC[1] < 0` and `AC[0] > AC[1] > AC[2] > AC[3]`, a market buy is issued.
   - **Bearish acceleration below zero:** if `AC[0] < 0` and `AC[1] < 0` and `AC[0] < AC[1] < AC[2]`, a market sell is issued.
   - **Zero-line crossings:** a downwards cross (`AC[0] > 0` and `AC[1] < 0`) triggers a buy, while an upwards cross (`AC[0] < 0` and `AC[1] > 0`) triggers a sell.

Signals are evaluated only after confirming that candles are finished, indicator values are formed and trading is enabled.

## Risk management

- **Stop-loss and take-profit:** configurable distances (in pips) converted to price units using the instrument’s step. Stops are recalculated on every fresh entry and remain unchanged until either hit or replaced by the trailing stop.
- **Trailing stop:** mirrors the EA logic. When unrealised profit exceeds `TrailingStop + TrailingStep` (both in pips), the stop price is moved to `Close - TrailingStop` for longs and `Close + TrailingStop` for shorts, while enforcing the required improvement before each step.
- **Position protection:** the built-in `StartProtection()` helper is invoked to let StockSharp guard against unexpected disconnections.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Order volume used for all market entries. It is normalised according to the security volume step and limits. |
| `StopLossPips` | Stop-loss distance in pips. A value of zero disables the stop-loss. |
| `TakeProfitPips` | Take-profit distance in pips. A value of zero disables the take-profit. |
| `TrailingStopPips` | Trailing stop distance in pips. Set to zero to disable trailing. |
| `TrailingStepPips` | Minimum extra profit (in pips) required before moving the trailing stop again. |
| `CandleType` | Timeframe of the candles used to compute the Accelerator Oscillator. |

## Implementation notes

- Price normalisation respects both the exchange tick size and three/five-digit Forex symbols by multiplying the point size by ten when appropriate.
- Indicator history is kept in a fixed-size buffer to replicate the original `ac[1] … ac[4]` comparisons without resorting to expensive collections or history queries.
- The strategy always exits before evaluating new entries on the same candle, matching the control flow of the MQL5 EA where `return` statements prevent immediate re-entry.
- Trailing stop values update both the internal trailing state and the effective stop price used for stop-loss checks, ensuring consistency with the EA’s `PositionModify` behaviour.

## Usage tips

1. Choose a candle timeframe that matches the market you trade (the original script was commonly used on intraday Forex charts).
2. Tune stop-loss, take-profit and trailing distances to the volatility of the chosen instrument; extremely tight values can lead to frequent whipsaws.
3. Enable risk controls on the connected broker side when possible, as the strategy relies on software-side exits.
4. Combine with portfolio-level money management if you intend to run multiple strategies simultaneously.
