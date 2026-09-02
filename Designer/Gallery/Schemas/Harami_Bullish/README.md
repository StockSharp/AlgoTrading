# Bullish Harami Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A harami is a candle that fits entirely inside the previous one, which says the side that just pushed the market has run out of breath. The original code measures that containment on the extremes rather than on the bodies, so what is recognized here is an inside bar that also changes colour: the previous candle went one way, the small candle inside it goes the other. That reversal is entered from flat and handed back to a simple moving average.

![schema](schema.svg)

## Strategy Overview

- Two candle pattern blocks carry custom patterns written exactly as the original code checks them: the previous candle has one colour, the current one the other, and its high and low both sit inside the previous range.
- A simple moving average of the closing price is not used to filter the entry at all; it is only the referee that decides when the trade is over.
- Entries are allowed only when the position is exactly flat, which is what makes a harami a reversal attempt rather than a way to add to a running trade.
- Exits are separate position modify blocks in close mode, so they never open anything by accident.

## Entry and Exit Rules

- **Long entry**: The bullish pattern block reports a bearish candle followed by a smaller bullish candle whose high is below the previous high and whose low is above the previous low, and the position is flat. The order buys one lot and opens a long.
- **Short entry**: The bearish pattern block reports a bullish candle followed by a smaller bearish candle contained the same way, and the position is flat. The order sells one lot and opens a short.
- **Exit**: A long is closed as soon as a candle closes below the moving average, a short as soon as a candle closes above it, both through position modify blocks in close mode, which matches the original exactly. The original also stops trading for five hundred candles after every order; no block keeps a bar counter between candles, so that pause is dropped and the diagram simply trades every pattern it finds while flat. The original works on one minute candles, and the packaged history is five minute data, so the diagram runs on five minute candles instead.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the simple moving average that closes the trades. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both pattern blocks, the moving average and a converter that reads the closing price.
- Two comparison blocks put the close on one side or the other of the moving average; the same two signals drive both closing blocks.
- One comparison block tests the position against a zero constant, and its output is shared by both entry conditions.
- Each logical AND joins one pattern with the flat check and triggers a position modify block that takes its volume from the shared constant.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
