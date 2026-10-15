# Spread-Filtered Entry Template Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The signal here is deliberately plain: a candle closes above its own open while price crosses above a slow moving average. What the diagram is really about is everything standing between that signal and the order — a spread read from the order book, a cooldown that keeps entries apart, and an order size computed from equity instead of a fixed number.

![schema](schema.svg)

## Strategy Overview

- Finished four-hour candles feed a 50-period simple moving average and two converters that pull the open and the close out of each candle.
- Candle colour is two comparisons: close above open is bullish, close below open is bearish.
- A Previous value block holds the candle from one step back and a second one holds the average, so a crossing is read as a pair of ordinary comparisons rather than as a separate indicator.
- Market depth supplies the best bid and the best ask, and a formula subtracts one from the other. The result is held in a variable that the candle releases, so the entry condition compares a spread belonging to the same moment as the rest of it.
- Strategy P&L feeds a variable that carries the realized result, a formula adds it to the start capital, and a second formula turns that equity into an order size: equity times the risk fraction, divided by the close price, rounded to three decimals.
- A counter built from a variable and a min(a + 1, n) formula measures candles since the last fill and blocks a new entry until eight of them have passed.
- Both entries are taken from a flat position only, so the diagram holds one position at a time and never adds to it.
- The exit is the opposite candle colour, and Position protection adds a 0.7% take-profit and a 0.5% stop-loss on top of it.

## Entry and Exit Rules

- **Long entry**: A bullish candle whose previous close sat at or below the previous average and whose close is above the current average, with the spread inside its limit, a flat position and the cooldown elapsed. Position modify buys at market with the computed volume.
- **Short entry**: A bearish candle whose previous close sat at or above the previous average and whose close is below the current average, under the same spread, flat-position and cooldown conditions. Position modify sells at market with the same computed volume.
- **Exit**: A bearish candle closes a long and a bullish candle closes a short: both conditions meet in a Combination that triggers one Position modify set to close the position, so neither direction needs its own exit block. Position protection watches the entry fills independently and can close the trade earlier at 0.7% profit or 0.5% loss.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 04:00:00 | Time frame of the candles the whole diagram works on. |
| SMA Length | 50 | Length of the simple moving average the close is measured against. |
| Spread Limit | 50 | Widest book, in price units, that still allows an entry. Raise it on instruments quoted with a wide spread. |
| Start Capital | 1000000 | Account size the equity calculation starts from; set it to the real size of the account before trading. |
| Risk Fraction | 0.3 | Share of equity committed to one position, as a fraction: 0.3 is thirty percent. |
| Cooldown Bars | 8 | How many finished candles must pass after a fill before the next entry is allowed. |
| Take Profit, % | 0.7 | Take-profit distance, in percent of the entry price. |
| Stop Loss, % | 0.5 | Stop-loss distance, in percent of the entry price. |

## Diagram Details

- The candle block feeds eight consumers: the average, both converters, the previous-candle block, the spread variable, the realized-result variable, the cooldown counter and the chart panel.
- The order book updates far more often than the candles, so its spread is not compared directly. A variable takes the latest value and releases it when the candle closes, which puts every term of the entry condition on the same clock.
- The realized-result variable starts at zero and only takes a value once the first trade is closed, which keeps the volume formula supplied from the very first candle.
- Both entry blocks share one volume formula, so long and short are sized by the same rule.
- The cooldown counter is reset by the strategy-fills block, which means any fill — an entry or a protective exit — starts the eight-candle wait again.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
