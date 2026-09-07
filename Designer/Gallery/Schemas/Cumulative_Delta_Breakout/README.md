# Cumulative Delta Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram builds a directional-volume delta from finished one-minute candles. A rolling Sum(100) supplies the breakout measure, SMA(20) filters entries, and the opposite delta threshold closes an open position.

![schema](schema.svg)

## Strategy Overview

- Each finished candle is separated into Open, Close, and TotalVolume values.
- Bullish and unchanged candles contribute positive volume, while bearish candles contribute negative volume.
- Sum(100) aggregates the latest one hundred directional-volume values, and SMA(20) follows closing prices; both emit formed values only.
- Position checks allow an entry only while flat and route an opposite delta event to the appropriate reducing exit.
- All four actions are one-unit market operations, and Strategy trades sends every fill to the chart.

## Entry and Exit Rules

- **Long entry**: When the rolling delta is at least +2, Close is above SMA(20), and the position is flat, buy one unit at market.
- **Short entry**: When the rolling delta is at most -2, Close is below SMA(20), and the position is flat, sell one unit at market.
- **Exit**: Reduce a long by one unit when delta reaches -2 or lower, and reduce a short by one unit when delta reaches +2 or higher. The exit gates do not use the SMA. The diagram has no stop-loss or take-profit.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles Series | 00:01:00 | Finished one-minute candles used for every calculation and decision. |
| Delta Sum Length | 100 | Number of directional-volume values retained by the rolling Sum indicator. |
| Delta Sum Source | Not set | No alternate indicator input field is selected; the directional-volume formula is connected directly. |
| SMA Length | 20 | Number of closing prices in the entry-filter moving average. |
| SMA Source | Not set | No alternate indicator input field is selected; Candle close is connected directly. |
| Delta Threshold | 2 | Absolute delta level used as +2 for bullish events and -2 for bearish events. |
| Order Volume | 1 | Fixed market volume for entries and reducing exits. |

## Diagram Details

- The directional-volume formula is positive when Close is greater than or equal to Open, so a doji contributes +TotalVolume; only Close below Open changes the sign.
- The delta is a rolling one-hundred-candle sum: each new value replaces the oldest value after the window is full.
- The positive and negative thresholds are produced from one exposed value, with a formula applying the negative sign to the bearish branch.
- A final evaluation pulse reaches the four AND gates after candle fields, indicators, comparisons, and the position snapshot have been refreshed.
- There is no bar-based waiting period. Flat, long, and short position gates prevent adding to a position and select the valid action for each candle.
- The chart receives six streams: candles, rolling delta, positive threshold, negative threshold, SMA(20), and all entry and exit fills.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
