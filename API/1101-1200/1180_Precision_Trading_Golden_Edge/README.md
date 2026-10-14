# Precision Trading: Golden Edge
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

An EMA crossover strategy with Hull direction, RSI momentum and ATR/high-low volatility confirmation. Its historical description targets gold scalping; the example works with the configured security and does not establish performance on gold.

## Signals and execution

Only finished candles are used; the default timeframe is five minutes. All seven indicators must be formed. The first fully formed candle establishes the previous EMA/HMA values without entering.

- Long: previous fast EMA ≤ previous slow EMA, current fast EMA > current slow EMA, current HMA > previous HMA, and RSI > `RsiLongThreshold` (55).
- Short: previous fast EMA ≥ previous slow EMA, current fast EMA < current slow EMA, current HMA < previous HMA, and RSI < `RsiShortThreshold` (45).
- Both directions also require positive close and ATR, `100 * ATR / close >= MinAtrPercent` (0.05), and `Highest(high) - Lowest(low) >= ATR * RangeAtrMultiplier` (3). The high/low window includes the current candle.

A qualifying cross opens `Volume` from flat or sends `Volume + abs(Position)` to fully reverse an opposite position. A same-side signal holds the position. Pending orders prevent duplicate submissions. Previous EMA/HMA values advance on every fully formed candle, so a rejected cross is not queued for a later entry. There is no six-hour cooldown. The example adds no stop-loss or take-profit; an opposite qualifying cross reverses the position.

## Indicator definitions and defaults

| Parameter | Default | Meaning |
|---|---:|---|
| `FastLength` / `SlowLength` | 14 / 40 | Native close EMAs; fast length must be less than slow length |
| `HmaLength` | 55 | `WMA(floor(sqrt(L)), 2 * WMA(floor(L/2), close) - WMA(L, close))`; length ≥ 2 |
| `RsiLength` | 14 | Native RSI with Wilder-smoothed close gains/losses |
| `RsiLongThreshold` / `RsiShortThreshold` | 55 / 45 | Strict directional thresholds; `0 <= short < long <= 100` |
| `AtrLength` | 14 | Native Wilder-smoothed true range, including previous-close gaps |
| `RangeLength` | 20 | Current-inclusive highest high and lowest low |
| `MinAtrPercent` | 0.05 | Inclusive minimum ATR as a percentage of current close; nonnegative |
| `RangeAtrMultiplier` | 3 | Inclusive minimum high-low span in ATR units; positive |
| `CandleType` | 5 minutes | Finished candles for every indicator |

WMA weights increase from 1 for the oldest sample to the window length for the newest. EMA starts from the native arithmetic-mean seed and then uses `2 / (length + 1)`. ATR uses a growing initial mean followed by Wilder smoothing. Periods are positive; Hull additionally needs its final square-root WMA to form.

C# and Python behavioral tests independently calculate EMA, HMA, RSI, ATR and rolling extrema over the bundled BTC/TON history. They check every submitted order, full reversals, rejected confirmations and signals less than six hours apart. These tests validate behavior, not profitability or suitability for live trading.

