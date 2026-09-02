# Volatility Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A channel built by hand: a simple moving average gives the centre, the Average True Range gives the width, and a close outside SMA plus or minus a multiple of ATR is treated as a move worth joining. Because the channel breathes with volatility, the same multiplier stays meaningful in quiet and in fast markets.

![schema](schema.svg)

## Strategy Overview

- SMA and ATR run over the same period on finished candles, so the channel is centred on the average price and scaled by the recent true range.
- Two formula blocks assemble the edges: the upper edge is SMA plus multiplier times ATR, the lower one is SMA minus the same amount.
- The strategy is always in the market: a breakout in the opposite direction reverses the position, and a protective stop closes it earlier if the move fails.

## Entry and Exit Rules

- **Long entry**: The candle closes above SMA plus multiplier times ATR and the position is not long. The order buys the base volume plus the absolute position, so a short is reversed into a long and a flat account opens a long.
- **Short entry**: The candle closes below SMA minus multiplier times ATR and the position is not short. The order sells the base volume plus the absolute position, so a long is reversed into a short and a flat account opens a short.
- **Exit**: There is no indicator-based exit. The position is reversed by the opposite breakout, or closed earlier by the protective stop-loss block attached to the trades of both entries.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Indicator period | 20 | Period shared by the SMA that centres the channel and by the ATR that sets its width. |
| ATR multiplier | 2 | How many ATRs away from the moving average the breakout edge sits. |
| Stop loss, % | 2 | Protective stop loss, in percent of the entry price. |
| Volume | 1 | Base order volume, in lots; the absolute position is added when reversing. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the two indicators and, through a converter, the close price used both for the comparisons and as the price source of the protection block.
- A constant holds the multiplier, and two formula blocks compute the upper and the lower edge from SMA, the multiplier and ATR.
- Two comparison blocks test the close against the edges, two more compare the position against zero, and each logical AND joins one of each into an entry.
- A formula block computes the reversal volume as base volume plus the absolute position and feeds both position modify blocks.
- The original protects the position with a stop of two absolute price units, which is calibrated for another instrument and would be hit instantly on a crypto price; the diagram uses a two-percent stop instead, which behaves the way the original intended on any instrument.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
