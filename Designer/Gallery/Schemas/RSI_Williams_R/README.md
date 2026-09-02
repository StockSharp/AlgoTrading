# RSI + Williams %R Double Cross Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two oscillators have to agree on the very same candle. The diagram buys only when RSI drops through 30 while Williams %R drops through -80 at the same time, and sells only when RSI rises through 70 while Williams %R rises through -20. A reading that is simply sitting inside the zone is not enough: the previous candle must still have been outside it, which is why both oscillators are also kept one candle back. The 180-bar cooldown of the original code is not reproduced, because on five-minute candles it would silence the strategy for fifteen hours after every trade.

![schema](schema.svg)

## Strategy Overview

- RSI 14 and Williams %R 14 are calculated on the same five-minute candles of one instrument.
- Previous-value blocks hold both oscillators one candle back, so a fresh break into the zone is told apart from a value that has been lying there for hours.
- Entries are taken from a flat position only, and the RSI midline at 50 is what brings the position back to flat.

## Entry and Exit Rules

- **Long entry**: RSI is below the oversold level while it was at or above it on the previous candle, and Williams %R is below its oversold level while it was at or above it on the previous candle; the position is flat. One lot is bought at market.
- **Short entry**: RSI is above the overbought level while it was at or below it on the previous candle, and Williams %R is above its overbought level while it was at or below it on the previous candle; the position is flat. One lot is sold at market.
- **Exit**: A long is closed as soon as RSI climbs back above the midline of 50, a short as soon as RSI falls back below it; both exits are position-closing blocks, so each one only touches the side that is actually open.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| RSI Length | 14 | Averaging length of the Relative Strength Index. |
| RSI Oversold | 30 | Level RSI has to break down through for a long signal. |
| RSI Overbought | 70 | Level RSI has to break up through for a short signal. |
| Williams %R Length | 14 | Look-back length of Williams %R. |
| Williams %R Oversold | -80 | Level Williams %R has to break down through for a long signal; the indicator runs from -100 to 0. |
| Williams %R Overbought | -20 | Level Williams %R has to break up through for a short signal. |
| RSI Midline | 50 | Neutral RSI level at which an open position is given up. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- Each oscillator feeds a pair of comparisons, one for its current value and one for its previous value, so a break through a level is described without a crossing block, which would let the two breaks come from different candles.
- Each logical AND collects five flags: the two RSI comparisons, the two Williams %R comparisons and a flat position taken from the position block compared with zero.
- Both entry blocks open a position only when there is none, and take their volume from one shared constant.
- Two more comparisons watch RSI against its midline and drive the position-closing blocks, which is the only exit in the diagram.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
