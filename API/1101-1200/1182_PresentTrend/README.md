# PresentTrend
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A causal ATR rail controlled by close RSI or volume-weighted typical-price MFI. Confirmed crossings against the rail's two-bar lag open, fully reverse or close positions.

## Rail calculation

Only finished candles are processed, five minutes by default. Native Wilder ATR and the selected oscillator share `Length` (14); the unselected oscillator is not registered. Wait until both selected indicators are formed.

For each formed candle, oscillator ≥50 selects the bullish rail; oscillator <50 selects the bearish rail:

- Bullish candidate: `low - Multiplier * ATR`. PresentTrend is the greater of the candidate and the previous line.
- Bearish candidate: `high + Multiplier * ATR`. PresentTrend is the lesser of the candidate and the previous line.
- The first formed line equals its selected candidate, without assuming earlier line values.

`Multiplier` defaults to 1. ATR includes gaps against the previous close and starts with a growing mean before Wilder smoothing. RSI uses Wilder-smoothed close gains/losses, retaining its prior result on an unchanged close. MFI uses `typical=(high+low+close)/3` times candle volume; rising typical prices contribute positive flow, falling ones negative flow, equal prices neither. MFI is `100 * positiveSum / (positiveSum + negativeSum)` over the last `Length` candles, or 100 when negative flow is zero.

## Signals and execution

A raw long cross at t requires `line[t-1] <= line[t-3]` and `line[t] > line[t-2]`; a raw short cross uses ≥ and < respectively. This needs four formed line values.

A long cross is confirmed only when an earlier raw short exists and is newer than the prior raw long, or no prior raw long exists. Short confirmation is symmetric. Record every raw signal after this comparison, including signals rejected for trading. The first raw signal cannot trade without an observed opposite signal. Repeated raw signals of the same direction do not create entries until an opposite signal intervenes.

With both directions enabled, a confirmed signal opens `Volume` from flat or submits `Volume + abs(Position)` for a full opposite-position reversal. A same-side signal holds. With only long enabled, a confirmed short closes the full long without opening short; short-only mode is symmetric. Signals in a disabled direction from flat do nothing. Pending orders block duplicates; line and raw-signal history still advance. Rejected signals are not queued, and there is no six-hour cooldown.

There is no separate price stop-loss or take-profit. The documented one-sided exit is an opposite confirmed signal, not an ATR price-stop order.

## Defaults and tests

| Parameter | Default |
|---|---:|
| `Length` | 14, positive shared ATR/oscillator period |
| `Multiplier` | 1, positive ATR distance |
| `IsUseMfi` | false, selects RSI; true selects MFI |
| `IsLongEnabled` / `IsShortEnabled` | true / true; at least one must be enabled |
| `CandleType` | 5 minutes |

C# and Python behavioral tests independently calculate ATR, RSI/MFI, the line and raw/confirmed crosses over the packaged BTC/TON archive. Every submitted order and filled position is checked for timing, side, full reversal or one-sided close; both oscillator modes and signals less than six hours apart are exercised. These checks do not establish profitability.
