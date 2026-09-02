# Volume Spike Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A candle that carries far more volume than the one before it usually means someone has just done something in size. This diagram waits for that jump, lets a simple moving average say whether the crowd is buying or selling, and joins in for as long as the volume keeps growing. The moment volume falls back below the previous candle, the trade is over.

![schema](schema.svg)

## Strategy Overview

- The volume of the candle is compared with the volume of the previous candle, not with an average of many candles, exactly as the original code does.
- The comparison is written as a multiplication rather than a division, so a candle with no volume at all cannot break the diagram.
- A twenty candle simple moving average of the closing price picks the side: above it the spike is bought, below it the spike is sold.
- Entries are made from a flat position only, and the exit needs neither the average nor the spike, only volume that has stopped growing.

## Entry and Exit Rules

- **Long entry**: The volume of the candle is at least the multiplier times the volume of the previous candle, the candle closed above the moving average and the position is flat. The order buys one lot at market.
- **Short entry**: The volume of the candle is at least the multiplier times the volume of the previous candle, the candle closed below the moving average and the position is flat. The order sells one lot at market.
- **Exit**: Both sides leave on the first candle whose volume is lower than the volume of the candle before it, through position modify blocks in close mode. The original strategy has no stop loss and no take profit, and neither does this diagram.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Spike Multiplier | 2 | How many times the previous candle's volume the current candle has to carry for the spike to count. |
| SMA Length | 20 | Averaging length of the simple moving average that picks the side of the entry. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds a converter for the volume, a converter for the closing price and the moving average block; a previous value block delayed by one candle supplies the volume of the earlier candle.
- A formula multiplies that earlier volume by the spike multiplier constant, and a comparison block checks the current volume against the result.
- Each logical AND joins the spike, the side chosen by the moving average and the flat position check, then triggers a position modify block set to open only.
- The falling volume comparison is wired straight into both closing blocks, which are in close mode and therefore do nothing while the diagram is flat. The original also pauses for five hundred candles after every trade and works on one minute candles; there is no counter block for such a pause and the packaged history is coarser than a minute, so the diagram runs on five minute candles and trades every spike.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
