# OBV Direction with Moving Average Filter Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

On-Balance Volume adds the volume of every up candle and subtracts the volume of every down candle, so its slope says which side is doing the trading. This diagram reads only the slope, one candle at a time, and lets a simple moving average of price decide which slope is worth acting on. The name of the original strategy speaks of a breakout, but its code compares OBV with its own previous value and nothing else, and the diagram follows the code.

![schema](schema.svg)

## Strategy Overview

- On-Balance Volume is calculated on finished candles and compared with its own value one candle back, which gives a plain rising or not rising verdict.
- A twenty candle simple moving average of the closing price splits the chart into an upper half and a lower half and decides the direction of the entry.
- An entry is made only from a flat position, so the two sides never fight each other inside one trade.
- The exit needs no moving average: the position is given up as soon as the volume flow turns against it.

## Entry and Exit Rules

- **Long entry**: On-Balance Volume is above its value on the previous candle, the candle closed above the moving average and the position is flat. The order buys one lot at market.
- **Short entry**: On-Balance Volume is at or below its value on the previous candle, the candle closed below the moving average and the position is flat. The order sells one lot at market. An unchanged OBV counts as not rising here, exactly as in the original code.
- **Exit**: A long is closed on the first candle where OBV stops rising, a short on the first candle where OBV rises again, both through position modify blocks in close mode. The original has no stop loss or take profit either.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the simple moving average that decides the direction of the entry. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the On-Balance Volume block, the moving average block and the converter that reads the closing price; a previous value block delayed by one candle supplies the earlier OBV, and two comparison blocks turn the pair into a rising and a not rising flag.
- Each logical AND joins the OBV flag, the position of price against the moving average and the flat position check, then triggers a position modify block set to open only.
- The same two OBV flags are wired straight into the closing blocks, which are set to close mode and therefore stay idle while the diagram is flat.
- The original strategy works on one minute candles and pauses for five hundred candles after every trade. The packaged history is coarser than one minute and the diagram has no bar counter, so it runs on five minute candles and trades every signal.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
