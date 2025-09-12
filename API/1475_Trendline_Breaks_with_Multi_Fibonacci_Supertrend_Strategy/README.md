# Trendline Breaks with Multi Fibonacci Supertrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy averages three SuperTrend calculations that use Fibonacci multipliers (0.618, 1.618, 2.618) and smooths the result with an EMA. Dynamic trendlines are built from swing highs and lows with slopes derived from ATR. A long trade is opened when price breaks above the upper trendline, the smoothed SuperTrend is rising and the +DI value exceeds −DI. Short trades mirror these rules.

## Details
- **Entry**: trendline breakout with DMI confirmation and SuperTrend agreement.
- **Exit**: price crossing back over the smoothed trend or hitting ATR‑based stop/target.
- **Indicators**: SuperTrend, ATR, Average Directional Index.
- **Type**: breakout, long and short.
