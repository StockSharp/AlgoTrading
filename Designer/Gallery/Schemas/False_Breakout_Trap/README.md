# False Breakout Trap Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades a return inside the previous twenty-bar range after price briefly breaks one of its boundaries. Two side-specific gates limit repeated entries, while an SMA provides immediate exits for open positions.

![schema](schema.svg)

## Strategy Overview

- Finished one-minute candles are separated into High, Low, and Close streams.
- Highest(20) and Lowest(20), each followed by Previous value with Shift = 1, define a range that excludes the current candle.
- A high above the previous range high followed by a close below it is a failed upside breakout; the mirrored low condition is a failed downside breakout.
- A Sell-side Flag and a Buy-side Flag share one 500-finished-candle N values block; each side can pass one event before the next shared reset.
- Market entries use a fixed volume of one from a flat position, and SMA(20) conditions reduce the open position by the same volume.

## Entry and Exit Rules

- **Long entry**: Low is below the previous twenty-bar low, Close is back above that boundary, and the Buy-side cooldown gate accepts the event. Buy one unit at market only while flat.
- **Short entry**: High is above the previous twenty-bar high, Close is back below that boundary, and the Sell-side cooldown gate accepts the event. Sell one unit at market only while flat.
- **Exit**: Reduce a long by one unit when Close is below SMA(20), or reduce a short by one unit when Close is above SMA(20). These exits are immediate and do not pass through the entry cooldown. The diagram has no stop-loss or take-profit.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles Series | 00:01:00 | Finished one-minute candles used for all range, signal, and exit calculations. |
| Highest Length | 20 | Number of candle highs in the rolling upper boundary. |
| Highest Source | Not set | No alternate indicator input field is selected; Candle high is connected directly. |
| Lowest Length | 20 | Number of candle lows in the rolling lower boundary. |
| Lowest Source | Not set | No alternate indicator input field is selected; Candle low is connected directly. |
| SMA Length | 20 | Number of closing prices in the moving average used for exits. |
| SMA Source | Not set | No alternate indicator input field is selected; Candle close is connected directly. |
| Cooldown N | 500 | Number of finished candles consumed before the shared cooldown reset is emitted. |
| Entry Volume | 1 | Fixed market volume for every entry and reducing exit. |

## Diagram Details

- Highest and Lowest receive numeric High and Low values, while SMA receives Close; all three indicators emit formed values only.
- Previous value shifts both range indicators by one update, so the candle being tested never contributes to its own boundary.
- A final evaluation tick reaches both failed-breakout AND gates only after the candle fields, indicators, position, and comparison results have been refreshed.
- Every raw failed-breakout event arms the shared N values block. Its output resets both Flags after 500 subsequent finished candles; each Flag suppresses further events on its own side until that reset.
- OpenPosition prevents a new order while a position is held. A raw failed-breakout event can still arm the cooldown during that time, because the cooldown is upstream of the position action.
- The two SMA exit gates check the position sign and use one-unit ReduceOnly market actions. The chart receives candles, both previous range boundaries, SMA, and all entry and exit fills.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
