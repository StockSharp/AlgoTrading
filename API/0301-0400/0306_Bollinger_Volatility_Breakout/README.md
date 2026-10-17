# Bollinger Volatility Breakout
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Uses finished 5-minute candles and waits for formed indicators and permission to trade. All moving statistics include the signal candle. `C` is its close, `M = SMA(C, 20)`, `D = population standard deviation(C, 20)`, `U = M + 2 × D`, `L = M − 2 × D`, and `A = ATR(14)` using Wilder smoothing of true range.

The high-volatility condition is `A >= SMA(A, 14) + 2 × population standard deviation(A, 14)`. The price levels are the upper, middle and lower Bollinger bands.

From a flat position, buy when high volatility is confirmed and `C >= U`; sell when it is confirmed and `C <= L`. The entry volume is `Volume`. A new entry is allowed only after the entry cooldown expires and while no order is pending. Entry requires `A > 0`.

`k = StopLossMultiplier`. Freeze the signal close and ATR at entry: the long stop is `C_entry − k × A_entry`, the short stop is `C_entry + k × A_entry`. On each finished candle, close the entire actual position with a market order if the close reaches the stored stop, a long has `C <= M`, a short has `C >= M`, or high volatility is no longer confirmed.

The cooldown blocks new entries for the next 84 finished candles after an order; exits remain active throughout. Stop checks occur at candle close, and market fills use the available execution price. The stop distance stays fixed while the position is held. There is no take profit or trailing stop.

## Default parameters

```text
BollingerPeriod = 20
BollingerDeviation = 2
AtrPeriod = 14
AtrDeviationMultiplier = 2
StopLossMultiplier = 2
CooldownBars = 84
CandleType = TimeSpan.FromMinutes(5).TimeFrame()
```
