# Inside Bar Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

An inside bar is a candle whose whole range fits inside the range of the candle before it: buyers and sellers have stopped pushing and the market is coiled. The diagram waits for the very next candle to leave that range and takes the breakout in the direction it leaves, then hands the trade over to a simple moving average, which decides when the move is over.

![schema](schema.svg)

## Strategy Overview

- Two candle pattern blocks carry a three-candle formula each: an unconstrained first candle, an inside bar that sits strictly inside it, and a breakout candle.
- The long formula asks the breakout candle for a high above the inside bar's high, the short formula asks it for a low below the inside bar's low.
- A simple moving average of the closing price is the only indicator: it takes no part in the entry and is used purely as the exit line.
- The position guard makes sure a breakout is acted on only when the diagram is flat, so one pattern gives one trade.

## Entry and Exit Rules

- **Long entry**: The candle pattern block reports an inside bar whose high has just been taken out by the following candle, and the position is flat. The order buys one lot and opens a long.
- **Short entry**: The candle pattern block reports an inside bar whose low has just been taken out by the following candle, and the position is flat. The order sells one lot and opens a short.
- **Exit**: A long is closed once a candle closes below the moving average, a short once a candle closes above it, both through position modify blocks in close mode, exactly as in the original strategy. What the diagram cannot reproduce is the original's open-ended wait: there the extremes of an inside bar are remembered and a breakout is accepted many candles later, while here the pattern block only sees a fixed window, so the breakout has to arrive on the candle right after the inside bar. That is the common case of the pattern, but the late breakouts are lost. The original's pause of several hundred bars between trades has no block of its own either and is left out.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the simple moving average that closes the trades. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both pattern blocks, the moving average and a converter that pulls the closing price out of the candle.
- Each pattern block holds three formulas, one per candle of the pattern, and reports true only on the candle that completes the breakout.
- The position block is compared with a zero constant, and each logical AND joins that guard with one of the two breakout signals.
- Both entry blocks send market orders and take their volume from one shared constant; the two exit blocks are triggered straight from the moving average comparisons and only act when there is something to close.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
