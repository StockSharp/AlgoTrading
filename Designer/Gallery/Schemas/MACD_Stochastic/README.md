# MACD + Stochastic Zero-Side Crossover Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A MACD crossover means something different depending on where it happens. This diagram accepts a bullish cross only while the MACD line is still below zero, which is where a new upswing starts, and a bearish cross only while it is still above zero. The Stochastic lines confirm the direction, the position must be flat before a trade, and a percent stop and take carry it out again.

![schema](schema.svg)

## Strategy Overview

- The trigger is the crossing of the MACD line and its signal line; the sign filter checks both the current and the previous value of the MACD line, so a bar that jumps across zero and across the signal line at once cannot be mistaken for a fresh cross.
- The Stochastic Oscillator is the second opinion: a long wants %K above %D, a short wants %K below it.
- Entries are allowed only from a flat position - the diagram never adds to a trade and never reverses on a signal; the stop and take are the only way out.
- The original is a port of a MetaTrader expert and measures its stop and take in pips, with three trading sessions and an optional multi-step trailing stop. The diagram converts the distances into percent of the entry price, and the session windows are left out because the default window covers the whole day.
- Two more simplifications: the Stochastic confirmation is wired in permanently, while in the code it is a switch that is off by default, and it compares the two lines as they are now instead of also checking how they stood four bars earlier. The original runs on four-hour candles; the diagram is scaled to five-minute candles to match the packaged sample history.

## Entry and Exit Rules

- **Long entry**: The MACD line crosses above its signal line, both the current and the previous MACD value are below zero, %K is above %D, and the position is flat. The order buys one lot at market.
- **Short entry**: The MACD line crosses below its signal line, both the current and the previous MACD value are above zero, %K is below %D, and the position is flat. The order sells one lot at market.
- **Exit**: The position protection block closes the trade at a fixed percentage from the entry price, either on the take profit or on the stop loss. There is no exit on an opposite MACD cross, exactly as in the original.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| MACD fast length | 12 | Length of the fast EMA inside MACD. |
| MACD slow length | 26 | Length of the slow EMA inside MACD. |
| MACD signal length | 9 | Length of the EMA that smooths MACD into the signal line. |
| Stochastic %K length | 5 | Lookback of the Stochastic %K line. |
| Stochastic %D length | 3 | Smoothing length of the %D line, the moving average of %K. |
| Volume | 1 | Order volume, in lots. |
| Take profit, % | 1 | Take profit distance, in percent of the entry price; it replaces the 100 pips of the original. |
| Stop loss, % | 1 | Stop loss distance, in percent of the entry price; it replaces the 100 pips of the original. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds MACD and the Stochastic Oscillator; four converter blocks pull the Macd, Signal, %K and %D values out of the two indicator values.
- A crossing block turns the MACD pair into the bullish trigger and a NOT block inverts it into the bearish one, while a previous-value block keeps the MACD line of the last candle for the sign check.
- Seven comparison blocks build the filters: four for the two zero tests, two for the Stochastic lines and one for the position against zero.
- Each logical AND joins five conditions and triggers a position modify block that sends a market order for the shared volume constant; both order blocks pass their own trade to the position protection block, which also reads the candle close as the current price.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
