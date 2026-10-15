# PresentTrend RMI Synergy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Native Relative Momentum Index entries aligned with a close-price SMA, followed by a ratcheted SMA/ATR trailing price stop. This example implements the historical description's momentum, mean and ATR model; no profitability result is asserted.

## Indicators and entry

Use finished five-minute candles by default. Native RMI uses `d[t] = close[t] - close[t-RmiMomentum]`, with lag 3. Over the last `RmiPeriod=21` momentum samples, let U be the mean of positive d (zero otherwise), and D the mean of negative d's absolute value (zero otherwise). `RMI = 100 * U / (U + D)`. A zero denominator yields no entry value. This is the native rolling-mean relative momentum calculation.

SMA and native Wilder ATR both use `SuperTrendLength=5`. ATR includes previous-close gaps and starts with a growing mean before Wilder smoothing. Entry waits for all three indicators:

- Long: RMI >60 and current close > SMA.
- Short: RMI <40 and current close < SMA.
- Only a flat position can enter; submit strategy `Volume` as a market order in an allowed direction. A held position ignores new entry signals.

`Direction` is an optional typed `Sides` filter: unset (`null` in C#, `None` in Python) means both, `Sides.Buy` means long only, `Sides.Sell` short only. It filters entries, while stops always close the actual held side. No string or numeric direction codes are needed.

## Trailing stop and execution

With `width = SuperTrendMultiplier * ATR` (default 4):

- At a long entry, initialize stop at `SMA - width`; each subsequent finished candle updates it to `max(previousStop, SMA - width)`.
- At a short entry, initialize stop at `SMA + width`; update to `min(previousStop, SMA + width)`.

The stop never loosens while that position remains open. It is initialized from the signal's SMA/ATR bands, independently of the eventual fill price. The retained SuperTrend parameter names configure these explicitly defined SMA-centered bands.

A positive bid ≤ the long stop or positive ask ≥ the short stop closes the entire actual position with a market order. Finished close is also checked after updating the stop; it can close immediately if it reaches the new level. Quotes between candles can trigger the stop. The new stop is applied to the current/future observed price, without applying it to the candle's earlier high/low. SMA/ATR can continue updating protection when the current RMI value is empty.

This is local price monitoring, requiring Level1 bid/ask for quote-based intrabar protection. Candle closes remain available when quotes are absent. The stop is not submitted as a native exchange stop order, and fills can differ from its threshold. Any unfinished order blocks duplicate orders until it is terminal; indicator and stop updates continue. The stop's exit candle cannot also enter, while a later eligible candle may re-enter. No EMA, six-hour cooldown, immediate opposite-signal reversal or separate take-profit.

## Defaults and checks

| Parameter | Default |
|---|---:|
| `RmiPeriod` | 21, positive |
| `RmiMomentum` | 3, positive close lag |
| `SuperTrendLength` | 5, positive shared SMA/ATR period |
| `SuperTrendMultiplier` | 4, positive ATR width |
| `Direction` | Unset: both directions |
| `CandleType` | 5 minutes |

C# and Python tests independently compute momentum sums, SMA, Wilder ATR and ratcheted stops using the existing BTC/TON archive. Every entry, executable-price stop exit and actual filled position is checked, including both directions, side filters, held opposite signals, tight intrabar stops and entries less than six hours apart.
