# SilverTrend ColorJFatl Digit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview

The SilverTrend ColorJFatl Digit strategy merges two classic MetaTrader systems into a unified high-level StockSharp strategy. The SilverTrend block identifies directional breakouts by measuring how far price travels inside a short Donchian-style channel. The ColorJFatl Digit block smooths price with a Jurik Moving Average (JMA) and evaluates its slope after rounding the output to the configured number of digits. Only when both subsystems agree on the direction does the strategy open or maintain a position. Whenever the signals diverge, the strategy exits to flat.

The design keeps the spirit of the original expert advisor while leveraging StockSharp's high-level API: candle subscriptions, indicator bindings, queue-based signal delays, and chart drawing helpers. Every step is heavily documented to make further research and optimisation simple.

## Strategy logic

### 1. SilverTrend breakout detector

* Uses a `Highest` and `Lowest` indicator with `SilverTrendLength + 1` candles to form the recent price channel.
* The channel is tightened by the `SilverTrendRisk` parameter: the higher the risk value, the closer the breakout thresholds sit to the channel midline (original formula `33 - risk`).
* When closing price breaks above the adjusted upper threshold, the SilverTrend block reports a bullish trend (`+1`). When it breaks below the lower threshold, the block reports a bearish trend (`-1`).
* A configurable delay (`SilverTrendSignalBar`) waits for `n` fully closed candles before the signal is considered valid, mimicking the MQL `SignalBar` logic.

### 2. ColorJFatl Digit confirmation filter

* A `JurikMovingAverage` smooths the applied price selected by `JmaPriceType`. All MetaTrader applied-price flavours are supported (close, open, median, typical, weighted, simple, quarter, trend-follow modes, and Demark calculation).
* The Jurik output is rounded to `JmaRoundDigits`, reproducing the discretised “digit” indicator behaviour.
* The slope sign of the rounded JMA becomes the trend signal. When the slope is positive, the filter emits `+1`; when negative, `-1`. Flat slopes inherit the previous state to avoid choppy toggling.
* As with SilverTrend, `JmaSignalBar` delays execution, requiring the slope to hold for the requested number of closed candles.

### 3. Trade execution

* **Entry:**
  * Go long when both SilverTrend and ColorJFatl blocks report `+1` and there is no existing long exposure.
  * Go short when both blocks report `-1` and there is no existing short exposure.
* **Exit:**
  * Close the current position immediately when signals diverge (e.g., one block says `+1`, the other `-1` or `0`).
  * Reversals automatically close the opposite exposure before opening the new position to avoid averaging down.
* Active orders are cancelled before reversals to keep the book clean.

## Parameters

| Name | Description |
| --- | --- |
| `SilverTrendCandleType` | Candle series used to compute the SilverTrend breakout channel. Defaults to H4 equivalent. |
| `SilverTrendLength` | Lookback length for the channel calculation (`SSP` parameter in the original EA). |
| `SilverTrendRisk` | Risk modifier tightening the breakout thresholds (`33 - risk`). Higher values react faster but whipsaw more. |
| `SilverTrendSignalBar` | Number of fully closed candles to wait before accepting a SilverTrend colour change. |
| `ColorJfatlCandleType` | Candle series feeding the Jurik filter. Can differ from the SilverTrend timeframe. |
| `JmaLength` | Length of the Jurik Moving Average. |
| `JmaSignalBar` | Delay (in bars) before acting on Jurik slope flips. |
| `JmaPriceType` | Applied-price mode for the Jurik input (close, open, median, trend-follow variants, Demark, etc.). |
| `JmaRoundDigits` | Number of decimals used when rounding the Jurik output, emulating the digitised indicator. |

## Implementation notes

* Signal delays are implemented with small FIFO queues rather than large historical arrays, ensuring the strategy remains memory efficient and faithful to the original Expert Advisor.
* The code never queries indicator buffers directly. Instead, it binds indicators through the high-level `SubscribeCandles().Bind(...)` API, following the guidelines in `AGENTS.md`.
* English inline comments explain every decision: when thresholds are recalculated, how slopes are computed, why orders are cancelled, and how consensus is enforced.
* Chart support is included: when a chart is available the strategy draws price candles, SilverTrend channel lines, and own trades to visualise live decisions.

## Usage tips

1. **Markets & timeframe:** The original system was designed for H4 forex charts. Crypto and commodity futures with clear swing behaviour also work well. For faster markets reduce `SilverTrendLength` and `JmaLength` cautiously.
2. **Optimisation:** Optimise both the breakout length (`SilverTrendLength`) and confirmation length (`JmaLength`) together—shortening only one leg usually creates conflicting signals.
3. **Applied price experiments:** Try the trend-follow price modes when working with Heikin-Ashi or Renko feeds; they often smooth noise better than pure closing prices.
4. **Risk control:** Combine the built-in exits with portfolio-level stops. Because both modules lag slightly, volatility spikes can still reach beyond the channel before the filter flips.
5. **Position sizing:** The strategy leaves volume management to the base `Strategy.Volume` property. Adjust it or integrate StockSharp’s money-management extensions if pyramiding or scaling is required.

## Further research ideas

* Add ATR-based stop-loss and take-profit protection through `StartProtection` once testing confirms the preferred thresholds.
* Feed higher timeframe candles (e.g., Daily) into the Jurik confirmation while keeping SilverTrend on H4 to introduce a trend filter.
* Combine with volume-based filters (On-Balance Volume, VWAP divergence) for additional confirmation before entries.
