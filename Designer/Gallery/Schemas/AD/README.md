# Accumulation/Distribution Line Trend Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Volume decides the direction here. The Accumulation/Distribution Line adds up where each candle closed inside its own range, weighted by traded volume, so a rising line means buyers absorbed the supply and a falling line means the opposite. The diagram compares the line with its own value one candle earlier and joins the side that volume supports, but only when the simple moving average agrees.

![schema](schema.svg)

## Strategy Overview

- The Accumulation/Distribution Line is fed the whole candle, because it needs high, low, close and volume together.
- A previous-value block keeps the reading from one candle back, so the slope of the line becomes a plain comparison instead of a second indicator.
- The simple moving average is the permission filter: volume may be flowing in, but the diagram buys only when the candle also closes above the average.
- Both entries carry the open-position condition and both exits the close-position condition, so one position is held at a time and never enlarged.

## Entry and Exit Rules

- **Long entry**: The A/D line is above its previous value, the candle closes above the simple moving average and the position is flat. The order buys the shared volume at market.
- **Short entry**: The A/D line is at or below its previous value, the candle closes below the simple moving average and the position is flat. The order sells the shared volume at market.
- **Exit**: The slope alone closes the trade, with no price condition attached: the line falling back closes a long, the line turning up closes a short. There is no stop loss and no take profit, exactly as in the original strategy.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| MA Period | 20 | Length of the simple moving average that decides which side of the market is allowed. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds three consumers at once: the A/D line, the moving average and the converter that pulls the closing price out of the candle.
- The A/D output goes both into a previous-value block and straight into two comparisons, so rising and falling are read off the same pair of numbers.
- Each logical AND joins the slope of the line, the side of the moving average and the flat-position check before it triggers an entry block.
- The two exit blocks hang directly on the slope comparisons and carry the close-position condition, which makes each of them act on one side only.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
