# Multi-Timeframe SMA Vote Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

One moving average turning up says little; three of them turning up together, on three different timeframes, is a trend worth trading. Each timeframe casts a vote of plus one, minus one or zero, Sync gathers the three votes into a single set, and a position is opened only when the vote is unanimous.

![schema](schema.svg)

## Strategy Overview

- Three candle blocks read the same instrument at five minutes, fifteen minutes and one hour, and each of them passes finished candles only.
- Every series feeds its own simple moving average, and a Previous value block keeps that same average as it stood one bar earlier.
- A formula turns the pair into a vote: plus one when the average is above where it was, minus one when it is below, zero when it has not moved.
- The five-minute and fifteen-minute votes are held in variables and released by the hourly vote, so all three lines reach Sync belonging to one and the same moment.
- Sync holds a line per vote and lets the three of them out together; without it the hourly reading would arrive an hour after the five-minute one and the three could never be compared.
- After Sync each vote is compared with zero, and two logical conditions ask the only question the diagram cares about: do all three point the same way.
- The verdict is latched onto the next finished five-minute candle, which is the beat every order is dated by, and Position modify opens at market from a flat position.
- The position, sampled on that same five-minute beat, is compared with zero: the opposite verdict against a live position closes it at market, and the new side is taken on the following candle.

## Entry and Exit Rules

- **Long entry**: All three votes are plus one on the hourly beat: the five-minute, fifteen-minute and hourly averages are each above their own value one bar back. The verdict is carried to the next finished five-minute candle, where Position modify buys the order volume at market, and only from a flat position.
- **Short entry**: All three votes are minus one on the same beat: every average is below its own value one bar back. On the next finished five-minute candle Position modify sells the order volume at market, again only from a flat position.
- **Exit**: There is no take-profit, stop-loss or timer. A position lives until the three timeframes line up the other way: the opposite verdict together with the position comparison closes it at market on the trading candle, and the entry of the new side follows on the next candle, once the position is flat again. A mixed vote changes nothing and leaves the position alone.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Fast Candles | 00:05:00 | Timeframe of the fastest vote, and the beat on which verdicts are acted upon and orders are dated. |
| Medium Candles | 00:15:00 | Timeframe of the middle vote. |
| Slow Candles | 01:00:00 | Timeframe of the slowest vote, and the beat on which the three votes are gathered and counted. |
| Fast SMA Length | 13 | Averaging length of the five-minute average. |
| Medium SMA Length | 13 | Averaging length of the fifteen-minute average. |
| Slow SMA Length | 13 | Averaging length of the hourly average. |
| Order Volume | 1 | Order size, in lots, used for both entry sides; an exit closes whatever the position holds. |

## Diagram Details

- All three averages emit formed, final values only, so a candle still being built can never move a vote.
- Reducing each timeframe to the sign of its slope is what makes the three comparable at all: an hourly average and a five-minute one live on different scales, but plus one and minus one do not.
- The two faster votes enter Sync through a variable released by the hourly vote. That is deliberate: lines that arrive one at a time would leave Sync holding a set that never completes, and nothing after it would ever fire again.
- Sync stamps the set it releases with the moment the set belongs to, which is behind the clock by the time the hour is closed. Nothing on the way to an order reads that stamp - the verdict is latched a second time onto the finished five-minute candle that carries the trade.
- Both entries are conditioned on an open position, so a verdict repeated on every five-minute candle cannot stack orders: while a position lives, the repeat is simply refused.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
