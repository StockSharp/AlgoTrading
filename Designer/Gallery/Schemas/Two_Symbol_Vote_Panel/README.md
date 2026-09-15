# Two Symbol Vote Panel Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A voting panel over two instruments. Every finished hour each instrument is scored against its own previous hour on seven price readings - open, high, low, close, midpoint, typical price and weighted close - and each reading that rose casts one vote up, each that fell one vote down. The two scores are added into a single verdict, and the traded instrument is bought or sold on that verdict alone. There is no indicator anywhere in the diagram: the whole decision is built out of candle prices, arithmetic and comparisons.

![schema](schema.svg)

## Strategy Overview

- Two candle blocks feed the diagram. One runs on the strategy's own instrument and is the one that gets traded; the other is pointed at a named instrument by a Security variable, so the second symbol is a setting rather than a wiring decision.
- Both series take finished candles only. A vote counted off a bar that is still forming would change several times inside the hour and would date the order it leads to with the opening of that hour.
- On each series Previous value holds the whole candle one step back, and converters read open, high, low and close out of the current candle and out of the held one. The candle is held first and its fields are read afterwards - that is the order that returns a value on every bar.
- Those eight numbers go into one Formula per instrument. Seven sign() terms, one per reading, each returning +1, 0 or -1, are added together: the result is votes up minus votes down and lands between -7 and +7. sign is what makes a comparison possible inside a formula, which has no way to return a true or false of its own.
- The reference score is sampled onto the traded bar by a Variable with input-as-trigger switched off: the score arrives on the input and waits there, the traded candle arrives on the trigger and lets it out. Two feeds never tick at the same instant, and this is what puts the second instrument on the clock of the instrument being traded.
- A second Formula adds the two scores into the panel verdict, between -14 and +14, and the verdict is drawn on the chart under the candles together with both scores that make it up.
- One threshold governs both sides. A comparison tests the verdict against it, a one-line formula turns its sign round, and a second comparison tests the verdict against that - so raising the setting tightens the long and the short case by the same amount.
- Position is snapped onto the bar by a variable and compared with zero three ways: flat admits an entry, long or short admits an exit. Four AND gates drive four Position modify blocks - two open, two close - and a second chart panel draws the reference instrument beside the traded one.

## Entry and Exit Rules

- **Long entry**: On a finished bar the panel verdict is above the threshold - the two instruments together cast a clear majority of their fourteen votes upward - and the position is flat. The long gate fires and Position modify buys the order volume at market under the Open position condition, so a following bar that repeats the verdict adds nothing to the position.
- **Short entry**: The mirror case: the verdict is below the negated threshold, the majority of the fourteen votes points down, and the position is flat. The short gate fires and the second Position modify sells the order volume at market, again under the Open position condition.
- **Exit**: There is no stop and no take profit. The panel that opened the position is also what gives it back: a verdict below the negated threshold while the position is long fires the exit gate, and Close position returns the whole position at market; a verdict above the threshold while the position is short does the same on the other side. Because the closing blocks close what is open rather than selling a fixed size, an exit can never flip the position - a turn of the panel first leaves the diagram flat, and the next bar that still reads the same way opens the new side.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Reference Instrument | TONUSDT@BNBFT | The second instrument whose bar casts the other half of the vote; it has to be available from the connection alongside the traded one. |
| Traded Candles | 01:00:00 | Time frame of the traded candles: the beat on which the verdict is read and orders are sent. |
| Reference Candles | 01:00:00 | Time frame of the reference candles. Keep it equal to the traded one, otherwise the two scores are counted over spans of different length and the sum stops meaning what it says. |
| Vote Threshold | 2 | How large a majority the two instruments must cast before a position is opened, and how far the verdict must swing the other way before it is given back. It is counted in votes, out of the fourteen the panel casts. |
| Order Volume | 1 | Order size, in lots. |

## Diagram Details

- Every order block is set not to require an online connection, so the diagram behaves the same way on history as on a live feed; left at its default the block would hold every transaction back on replayed data.
- The entries carry the Open position condition and the exits the Close position condition with the opposite side. Without a condition a block would act on every position change it is triggered through, and one verdict would turn into a stream of orders.
- Every constant - the zero, the threshold and the order volume - is triggered by the traded candle. A variable emits on its trigger and not when its value is set, so an untriggered constant would leave the comparison next to it silent for the whole run.
- A reading that repeats exactly casts no vote at all: sign returns zero on an unchanged value, so only real movement is counted, and a flat hour on one instrument simply leaves the other one to decide.
- The threshold is what turns the panel from a hair trigger into a filter. At zero the diagram acts on the slightest majority and reverses almost every bar; raised, it waits until the two instruments agree strongly enough, and the position is held through the disagreements in between.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
