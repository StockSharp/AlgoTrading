# Weighted Signal Basket with Expiring Limits
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram combines an RSI-zone vote and an EMA-position vote into one score from −3 to +3. A threshold crossing while flat registers a limit at the finished close, Combination<Order> sends that exact order into a twelve-candle N values timer and Order cancellation, and fills start 1.2%/0.8% Position protection.

![schema](schema.svg)

## Strategy Overview

- RSI below 30 contributes +2, RSI above 70 contributes −2, and the middle zone contributes zero.
- Close above EMA(20) contributes +1 and close below it contributes −1; Formula sums the two weighted votes.
- Current and Previous value comparisons detect a fresh crossing of +1 for a long or −1 for a short, with Position == 0 required on both sides.
- Buy and sell entry orders share the finished close and volume one; their Order outputs merge through Combination while their MyTrade outputs feed Position protection.
- N values counts twelve finished candles after registration and asks Order cancellation to remove the current unfilled order.

## Entry and Exit Rules

- **Long entry**: The weighted score becomes at least +1 after its previous value was below +1, while the position is flat. Order registering places a buy limit at the finished close.
- **Short entry**: The weighted score becomes at most −1 after its previous value was above −1, while the position is flat. Order registering places a sell limit at the finished close.
- **Exit**: An executed entry is protected at +1.2% and −0.8% from the fill. An unfilled limit is passed as an Order object to Order cancellation after N values has counted twelve completed candles.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 00:05:00 | Finished-candle interval; five minutes adapts the C# 60-minute default for monthly replay. |
| RSI Length | 14 | RSI period; the executable C# default is 21, while the compact gallery vote uses 14. |
| EMA Length | 20 | EMA period; the executable C# default is 50, while the gallery uses 20. |
| RSI Weight | 2 | Magnitude of the oversold and overbought RSI vote. |
| Trend Weight | 1 | Magnitude of the vote from close relative to EMA. |
| Replay Signal Threshold | 1 | Crossing boundary used in replay; the blueprint value 2 is exposed as a documented tuning alternative. |
| Cancel After N Candles | 12 | Completed candle values counted before attempting to cancel an unfilled entry. |
| Take Profit, % | 1.2 | Percentage gain from the fill used by Position protection. |
| Stop Loss, % | 0.8 | Percentage loss from the fill used by Position protection. |
| Order Volume | 1 | Volume of each buy or sell limit entry. |

## Diagram Details

- The executable C# defaults are 60-minute candles, RSI(21), EMA(50), threshold 2 behavior, and a four-candle signal cooldown. It also scores candle direction and intermediate RSI zones. This compact gallery diagram intentionally uses five minutes, RSI(14), EMA(20), two votes, and no separate cooldown.
- The reviewed blueprint proposed threshold 2. With only its two retained votes, March replay produced no orders because oversold RSI normally coincided with price below EMA and the votes cancelled; the transparent replay default is therefore 1. The parameter remains exposed for restoring 2 or further tuning.
- The adjacent README describes eight weighted patterns, pending-order offsets, expiration, and protection inherited from the original expert. The current C# sample implements three score families and market entries, without pending expiration or protection blocks.
- Using a close-priced limit instead of the C# market order is deliberate: it gives Combination, N values, and Order cancellation a real order lifecycle. Price shrinking is disabled because the replay security has no price step.
- Unlike C# gates Position <= 0 / >= 0, the diagram is flat-only and does not reverse an opposite position. Percentage protection 1.2/0.8 is a gallery risk example rather than executable C# logic.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
