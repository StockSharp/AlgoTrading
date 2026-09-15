# Relative Strength Against A Reference Instrument Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram builds a synthetic instrument from the ratio of the traded security to a reference security, measures how far that ratio has run away from its own 60-period simple moving average, and trades the traded security on the result. A ratio one percent above its average means the traded security is outperforming the reference and the diagram goes long; one percent below means it is lagging and the diagram goes short. Orders always go to the traded security; the synthetic instrument only takes part in the decision.

![schema](schema.svg)

## Strategy Overview

- A Security index block builds a synthetic instrument from the expression `BTCUSDT@BNBFT/TONUSDT@BNBFT`. Its candles are the price of one instrument expressed in units of the other, so a rising series means the traded security is gaining on the reference.
- Finished five-minute candles of that synthetic instrument feed a Converter that reads the close price and a formed-only SimpleMovingAverage of length 60, which is five hours of the same series.
- A Formula divides the ratio close by its average and subtracts one, producing relative strength as a fraction: `+0.01` means the ratio stands one percent above its five-hour average, `-0.01` one percent below.
- A separate series of finished five-minute candles of the traded security drives the decision cycle. A Variable with its input used only as storage holds the latest relative strength and releases it on the traded candle, so every comparison and every order carries the timestamp of the traded bar rather than that of the synthetic one.
- Two Comparison blocks test the released value against the threshold and against its negative, produced by a Formula `0 - a` so that a single exposed number governs both sides symmetrically.
- The current Position is compared with zero twice, giving `Position <= 0` and `Position >= 0`. Two Logical condition blocks combine each strength signal with the matching position test, so a side that is already open cannot receive another entry.
- From flat, a Position modify block set to Open position buys or sells the fixed Order Volume at market. There is no stop-loss and no take-profit block.
- A position that is open against the fresh signal is flattened first by a Position modify block set to Close position, which leaves the reversal to the next qualifying candle.

## Entry and Exit Rules

- **Long entry**: On a finished traded candle, when the released relative strength is greater than the Strength Threshold and the position is not long, the long gate fires. From flat, the Open position block buys the Order Volume at market. From a short position the entry is refused for that bar, because the close action is what runs first; the long is opened on the next candle that still shows outperformance.
- **Short entry**: On a finished traded candle, when the released relative strength is less than the negative of the Strength Threshold and the position is not short, the short gate fires. From flat, the Open position block sells the Order Volume at market. From a long position the entry is refused for that bar, because the close action is what runs first; the short is opened on the next candle that still shows underperformance.
- **Exit**: There is no independent exit rule, no stop-loss and no take-profit. A position leaves only on a signal in the opposite direction: outperformance closes a short, underperformance closes a long. The closing action takes its size from the open position, so the account is flat afterwards, and the opposite side opens on the following candle if the signal is still there.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | Expression the synthetic instrument is built from. The first instrument is the numerator and the second the reference denominator, so the series rises when the numerator gains on the reference. Change it to measure the traded security against a different reference. |
| Ratio Candles | 00:05:00 | Time frame of the synthetic instrument's candles. It has to match the traded series, because the released strength is one value per traded bar. |
| Traded Candles | 00:05:00 | Time frame of the traded security's candles. Every comparison, every entry and every exit is evaluated once per finished candle of this series. |
| Reference Average Length | 60 | Number of bars in the simple moving average of the ratio. Sixty five-minute bars measure strength over the last five hours; a longer average measures a slower, rarer divergence and produces fewer trades. |
| Strength Threshold | 0.01 | Distance from the average, as a fraction, that the ratio has to travel before a side is taken. `0.01` is one percent, applied above the average for long entries and below it for short entries. Lower it to trade more often, raise it to demand a wider divergence. |
| Order Volume | 1 | Fixed quantity of every entry. The closing actions ignore it and take their size from the open position instead. |

## Diagram Details

- The [Security index](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/index.html) block carries the expression the synthetic instrument is built from and feeds the Security input of one [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block. A second Candles block, left on the strategy security, supplies the traded series. Both are set to finished candles only, so no forming bar can date an order to the opening of its own bar.
- A [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) reads the close price of the synthetic candle and a formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) averages the same series over 60 bars. The [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) `a / b - 1` turns the pair into one signed fraction, and a second Formula `0 - a` mirrors the threshold onto the weak side.
- A synthetic candle is assembled from two feeds and completes after an ordinary candle of the same minute. A [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) therefore stores the strength on its input and emits it only on the trigger from the traded candle, which is what keeps the order timestamps on the traded clock. The constants for the threshold, zero and the order volume are triggered from the same candle so that every [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) sees both of its operands within one evaluation.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) is compared with zero on each traded candle, and two [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) blocks join the strength result with the position result. Only a `true` result reaches a trading block; a `false` comparison is discarded at the trigger input.
- Four [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks act by market order. The two entry blocks use the Open position condition, so they act only from a flat account and cannot repeat while a side is open. The two exit blocks use the Close position condition, which sizes itself from the open position and does nothing when the account is already flat or already on the requested side.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
