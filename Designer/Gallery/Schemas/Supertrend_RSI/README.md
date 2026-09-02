# Supertrend + RSI Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A trend-following diagram with an oscillator brake. SuperTrend, an ATR band that trails the price and flips sides with it, decides the direction, while RSI decides whether the move has room left: a long is only taken while RSI is still below its midline, a short only while it is still above. The exit is not a signal at all, it is a fixed percent take-profit and stop-loss placed on the entry trade.

![schema](schema.svg)

## Strategy Overview

- SuperTrend is built from a ten-period ATR times three, so the line ratchets behind the price and only turns when the close breaks through it.
- RSI is used as a brake, not as a reversal signal: the entry is allowed while the oscillator is on the calm side of the fifty line, which keeps the diagram out of moves that are already stretched.
- Entries are taken only from a flat position, both through an explicit comparison of the position against zero and through the open-position condition on the order blocks.
- The whole exit is delegated to a protection block carrying a two percent take-profit and a one percent stop-loss, exactly the pair the original strategy starts.

## Entry and Exit Rules

- **Long entry**: The close is above the SuperTrend line, RSI is below the fifty midline, and the position is flat. The order buys the shared volume at market and the protection block immediately arms a take-profit and a stop-loss on the resulting trade.
- **Short entry**: The close is below the SuperTrend line, RSI is above the fifty midline, and the position is flat. The order sells the shared volume at market, again with the protection block arming the two exits.
- **Exit**: There is no signal-based exit and no reversal: the position is closed by whichever of the two protective orders is hit first, the two percent take-profit or the one percent stop-loss.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SuperTrend ATR Period | 10 | ATR length inside SuperTrend; longer values widen the band and make the flips rarer. |
| SuperTrend Multiplier | 3 | ATR multiplier of SuperTrend, the distance of the trailing line from the median price. |
| RSI Length | 14 | Averaging length of the Relative Strength Index. |
| RSI Midline | 50 | The RSI level the entry filter is measured against; the original code compares against fifty rather than against the oversold and overbought levels it declares. |
| Take Profit, % | 2 | Take-profit distance from the entry price, in percent. |
| Stop Loss, % | 1 | Stop-loss distance from the entry price, in percent. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds SuperTrend, RSI and a converter that reads the close price of the same candle.
- A comparison of the close against the SuperTrend output gives the up trend flag; a logical NOT of it gives the down trend flag, which is why the two directions never fire on the same candle.
- One shared constant of fifty serves both RSI comparisons, so moving the midline moves both filters at once.
- Each logical AND joins three conditions — trend, oscillator and a flat position — and triggers a position modify block that also carries the open-position condition.
- Both modify blocks pass their own trade to the protection block, which places the take-profit and stop-loss orders and is priced off the close of the running candle.
- The hundred-bar pause the original code keeps between trades is not reproduced: the available blocks have no bar counter, so entries resume as soon as the protection has flattened the position.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
