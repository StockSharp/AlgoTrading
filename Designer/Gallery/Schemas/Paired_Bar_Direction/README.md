# Paired Bar Direction Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two instruments, one bar. The diagram reads the direction of the same five-minute candle on the traded instrument and on a second, reference instrument, and buys only where the two disagree: the reference bar closed up while the traded bar closed down. The position is given back as soon as price closes above the high of the previous bar.

![schema](schema.svg)

## Strategy Overview

- Two candle blocks feed the diagram. The traded one runs on the strategy's own instrument; the reference one is pointed at a named instrument by a Security variable, so the pair is a setting rather than a wiring decision.
- Both series are set to finished candles only, so an unfinished bar can never move the decision.
- Sync holds one line per instrument and lets both candles out together on the five-minute beat. Two feeds arrive independently, and only after that hold do the two candles belong to the same bar - which is the one thing that makes comparing them meaningful.
- Converters take the open and the close out of each released candle, reducing every instrument to the two numbers that say which way its bar went.
- Two comparisons read those numbers: reference close above reference open means the reference bar closed up, traded close below traded open means the traded bar closed down.
- Previous value keeps the traded candle one step back, and a converter reads the high out of it. The block holds the candle itself and the field is read afterwards, which is the order that gives a value on every bar.
- The position is snapped onto the bar by a variable that emits on the candle trigger, then compared with zero twice: at or below zero admits an entry, above zero admits an exit.
- Two logical AND gates drive two Position modify blocks - one opens with the Open position condition, the other closes with Close position - and the chart panel shows the traded candles, the exit level, the orders and the fills.

## Entry and Exit Rules

- **Long entry**: On a released bar the reference instrument closed above its own open, the traded instrument closed below its own open, and the position is at or below zero. The AND gate fires and Position modify buys the order volume at market with the Open position condition, so a bar that repeats the pattern while the position is already open adds nothing.
- **Short entry**: There is no short side. The diagram is long only: a down bar on the traded instrument is read as the discount to buy, never as a reason to sell.
- **Exit**: While the position is above zero, a released bar whose close is above the high of the previous bar fires the exit gate, and the second Position modify closes at market with the Close position condition. There is no stop and no take profit: the previous bar's high is the whole exit rule, and because the block closes what is open rather than selling a fixed size, an exit cannot flip the position short.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Reference Instrument | TONUSDT@BNBFT | The instrument whose bar direction confirms the entry; it has to be available from the connection alongside the traded one. |
| Reference Candles | 00:05:00 | Time frame of the reference candles. |
| Traded Candles | 00:05:00 | Time frame of the traded candles. |
| Sync Interval | 00:05:00 | The beat on which Sync releases both instruments; keep it equal to the candle time frame, otherwise the released pair is not the bar the comparisons assume. |
| Order Volume | 1 | Order size, in lots. |

## Diagram Details

- Both Position modify blocks are set not to require an online connection, so the same diagram behaves identically on history and on a live feed.
- The entry carries the Open position condition on purpose. Without it the block would act on every position change it is triggered through, and a single signal would turn into a stream of orders.
- Every constant in the diagram - the zero and the order volume - is triggered by the released candle. A variable emits on its trigger, not on its own value, so an untriggered constant would leave the comparisons next to it silent for the whole run.
- Both ends of each Sync line are linked. A value sent into the block and never taken out leaves it waiting for a line that never closes, and the strategy would not start at all.
- The exit level is drawn on the chart as its own line, so the bar that closes above it can be read straight off the panel next to the fill that followed.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
