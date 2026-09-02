# Stochastic %K/%D Cross in Extreme Zones Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A crossover of the two Stochastic lines is a common but noisy signal, so this diagram accepts it only where it means something: a bullish cross has to happen while %K is still in the oversold zone, a bearish one while %K is still overbought. Every accepted signal reverses the position, so the diagram is either long or short and never merely waiting.

![schema](schema.svg)

## Strategy Overview

- One Stochastic Oscillator block supplies both lines; converter blocks split its value into %K and %D.
- A crossing block compares the two lines: its signal marks a bullish cross, and the same signal inverted by a NOT block marks a bearish one.
- The zone filter is a plain comparison of %K against the oversold and overbought constants, so a cross in the middle of the range is ignored.
- The order volume is the base volume plus the absolute value of the position, which closes the opposite side and opens the new one with a single market order.
- Despite the folder name of the original strategy there is no RSI in it, and there is no stop loss either; the pause of five candles it keeps after a trade has no block equivalent and is left out.
- The original works on fifteen-minute candles; the diagram is scaled to five-minute candles to match the packaged sample history.

## Entry and Exit Rules

- **Long entry**: %K crosses above %D while %K is below the oversold level and the position is not already long. The order buys the base volume plus any open short, which reverses the position into a long.
- **Short entry**: %K crosses below %D while %K is above the overbought level and the position is not already short. The order sells the base volume plus any open long, which reverses the position into a short.
- **Exit**: There is no separate exit block: the position is held until the opposite cross appears in the opposite zone, and that order both closes the old trade and opens the new one.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| %K Length | 14 | Lookback of the Stochastic %K line. |
| %D Length | 3 | Smoothing length of the %D line, the moving average of %K. |
| Oversold | 20 | Level below which a bullish cross is accepted as a buy. |
| Overbought | 80 | Level above which a bearish cross is accepted as a sell. |
| Volume | 1 | Base order volume, in lots; the reversal adds the open position on top of it. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds a single Stochastic Oscillator, and two converter blocks read the %K and %D lines out of its value.
- The crossing block fires only on the candle where the lines swap places, which is what keeps the diagram from trading every bar the lines are apart.
- Each logical AND joins the cross, the zone comparison and a position check before triggering a position modify block.
- A formula block adds the base volume to the absolute value of the position and feeds both order blocks, so one market order performs the whole reversal.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
