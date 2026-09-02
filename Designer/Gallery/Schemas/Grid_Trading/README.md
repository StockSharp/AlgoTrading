# Grid Trading Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The diagram turns price into a ladder: the close of every candle is rounded down to a multiple of the grid step, and only a move onto a new rung counts as a signal. A step up buys, a step down sells, so the position always follows the direction in which the grid was crossed.

![schema](schema.svg)

## Strategy Overview

- The close price is discretised by the formula floor(Close / GridStep) * GridStep, which gives the rung the market currently stands on.
- A previous-value block remembers the rung of the last candle, so the diagram compares rungs instead of raw prices and ignores every move inside one cell of the grid.
- The order volume is the open position plus the base volume, so a signal against an open position reverses it with a single market order.
- The original strategy runs on four-hour candles and closes a position at an absolute profit of 2000 price units; here it works on five-minute candles and the target is a percentage of the entry price, which keeps it meaningful on any instrument.

## Entry and Exit Rules

- **Long entry**: The new grid rung is above the previous one and the position is not long. The order buys the base volume plus any open short, which turns the position into a long of one base volume.
- **Short entry**: The new grid rung is below the previous one and the position is not short. The order sells the base volume plus any open long, which turns the position into a short of one base volume.
- **Exit**: The position protection block closes the position on a take profit of the configured percentage; there is no stop loss, as in the original. Otherwise the position is held until the price crosses into the next grid cell and the opposite signal reverses it.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Grid Step | 500 | Height of one grid rung, in price units of the instrument. |
| Take Profit, % | 3 | Take profit, as a percentage of the average entry price. |
| Volume | 1 | Base order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds a converter that reads the close price, and a formula block rounds that price down to the grid.
- A previous-value block delays the rung by one candle; two comparison blocks decide whether the rung has gone up or down.
- Two comparisons of the position against zero are joined with the grid signals in logical AND blocks, so a rung change never adds to a position already held in that direction.
- A second formula computes |Position| + Volume and feeds the volume input of both position modify blocks, which is what makes a reversal a single order.
- Own trades of both modify blocks go into the position protection block, whose price input is the close of finished candles.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
