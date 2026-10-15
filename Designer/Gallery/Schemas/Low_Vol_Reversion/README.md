# Low Volatility Reversion Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Mean reversion works when the market is going nowhere and hurts when it is trending, so this diagram only takes the trade while the market is quiet. Quiet is defined without any absolute number: the current Average True Range is compared with its own smoothed average, and only when it is below a share of that average is a position opened.

![schema](schema.svg)

## Strategy Overview

- Volatility is measured relative to itself: an AverageTrueRange feeds a SmoothedMovingAverage, and the ratio of the two is the whole regime filter, so the diagram carries over to any instrument without recalibration.
- SmoothedMovingAverage uses a recursive formula: average times length minus one plus the new value, divided by length.
- The fair value is a plain SimpleMovingAverage: a close below it is bought, a close above it is sold, but only in the quiet regime and only from a flat position.
- The diagram runs on the five-minute candles supplied with the packaged history.

## Entry and Exit Rules

- **Long entry**: The Average True Range is below the quiet level, the close is under the moving average and the position is flat. The order buys the configured volume.
- **Short entry**: The Average True Range is below the quiet level, the close is above the moving average and the position is flat. The order sells the configured volume.
- **Exit**: A long is closed once the close crosses back above the moving average, a short once it crosses back below. The exits deliberately ignore the volatility filter, so a trade is always given back even when the market has woken up. There is no stop loss and no take profit.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the moving average that serves as fair value. |
| ATR Length | 14 | Averaging length of the Average True Range, the current volatility. |
| ATR averaging length | 20 | Length the Average True Range is smoothed over to get its own average. |
| Quiet threshold, % | 80 | Share of the average volatility, in percent, below which the market counts as quiet. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the close-price converter, the moving average and the Average True Range; the range then feeds a second indicator block that smooths it.
- One formula block turns the smoothed range and the exposed percentage into the quiet level, and a comparison block puts the raw range against it.
- Two comparison blocks decide which side of the moving average the close is on, and they are reused: the one that opens a long also closes a short.
- The two entry ANDs join three conditions each, price, volatility and a flat position, while the two exit ANDs join only price and the position, which is what makes the exits work in any regime.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
