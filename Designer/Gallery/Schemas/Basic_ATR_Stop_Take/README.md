# ATR Stop and Take Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A short lesson in volatility-scaled risk. A close crossing the 50-period EMA opens the trade, the close of that candle is stored as the entry price, and from then on the diagram measures how far price has moved away from it in units of the Average True Range. One ATR multiple closes the trade as a loss, another closes it as a profit, so the exit distance grows in quiet markets and shrinks in busy ones instead of being a fixed number of ticks.

![schema](schema.svg)

## Strategy Overview

- Only one instrument and one candle series are used; the 50-period EMA gives the direction and the 14-period ATR gives the yardstick for the exits.
- Two variable blocks make the entry price: the first takes the close of the candle that produced the signal, the second re-issues it on every following candle so the exit conditions can be tested continuously.
- Two formula blocks turn the distance from the entry price into ATR multiples, one measured in favour of a long and one in favour of a short, so the same two thresholds serve both directions.
- The exit is a market order on a finished candle, exactly as in the source strategy: there is no resting stop order sitting on the exchange, so an intrabar spike does not take the trade out.

## Entry and Exit Rules

- **Long entry**: The close crosses above the EMA while the position is flat. One lot is bought and the close of that candle becomes the entry price.
- **Short entry**: The close crosses below the EMA while the position is flat. One lot is sold and the close of that candle becomes the entry price.
- **Exit**: The position is closed on the first finished candle where price has moved StopFactor ATR against the entry price or TakeFactor ATR in its favour. Both position modify blocks are set to close, so each one only fires on the side it belongs to.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| EMA Length | 50 | Length of the exponential moving average the close has to cross. |
| ATR Length | 14 | Length of the Average True Range that scales the stop and the target. |
| Stop, ATR | 1.5 | Stop distance, in ATR: the loss that closes the trade. |
| Take, ATR | 2 | Target distance, in ATR: the profit that closes the trade. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:15:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds a converter for the close price, the EMA and the ATR; a crossing block compares the close against the EMA and a logical NOT turns its downward crossing into the short signal.
- The current position is compared against a zero constant, and each logical AND joins that check with one crossing so a new trade is only opened from flat.
- The entry price is held by a pair of variable blocks; the second is triggered by the candle series, which is why it is the last link the candle block sends and why the exit is measured against the right price on the entry candle itself.
- Four comparison blocks test the two ATR distances against the stop and target constants, two logical OR blocks merge them, and two position modify blocks set to close send the exit orders.
- The source strategy waits six candles between trades. A counter like that has no equivalent among the blocks, so the diagram omits it and takes the next crossing straight away.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
