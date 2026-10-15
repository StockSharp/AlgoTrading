# Cross-Instrument MACD Comparison Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The diagram trades one instrument and decides on two. A MACD is measured on the traded instrument and on a reference instrument, and what is compared is not their prices but their momentum: when the traded side is the weaker of the two on the fast reading while still the stronger one on the slow reading, the diagram treats it as a gap that has yet to close and buys it. The mirror image sells.

![schema](schema.svg)

## Strategy Overview

- The Index block builds a synthetic security from an expression and hands it to a second candle block, which is how a second instrument enters a diagram at all.
- Both legs are subscribed as candle series of the same time frame: one on the strategy security, one on the security Index produced.
- Sync holds a line per leg and lets the two out together, so a reading of the traded instrument and a reading of the reference instrument always describe the same bar rather than whichever series happened to arrive.
- Each leg gets its own MACD, and converters pull three numbers out of it: the MACD line, the signal line and the close price of the candle underneath.
- Two formulas per leg turn those into percentages of that leg's own price — the histogram, MACD minus signal, and the signal line itself. Comparing the raw values would be meaningless when the two instruments are priced orders of magnitude apart.
- Four variables read the four percentages on the traded candle, so both the comparison and the order that follows it are timed by the instrument being traded and not by whichever leg closed last.
- Comparisons put the legs side by side — histogram against histogram, signal against signal — and four logical conditions add the position state, one gate per action: open long, open short, close long, close short.
- Entries are market orders of a fixed volume taken only from a flat position; the opposite reading closes, and Position protection carries a take-profit and a stop-loss in percent of the entry price.

## Entry and Exit Rules

- **Long entry**: On a traded candle the traded instrument's histogram sits below the reference histogram while its signal line sits above the reference signal line, and the position is flat. The fast reading says the traded side is behind, the slow reading says it is still in front, and the diagram reads the pair as a lag due to be made up. Position modify buys the order volume at market.
- **Short entry**: On a traded candle the traded instrument's histogram sits above the reference histogram while its signal line sits below the reference signal line, and the position is flat. Position modify sells the order volume at market.
- **Exit**: There are two ways out and either can come first. The mirror-image reading closes the position: a long goes when the histogram moves ahead of the reference and the signal line falls behind it, a short on the opposite pair. Each is a Position modify set to close, so the volume comes from the position itself and nothing is reversed inside one order — the diagram returns to flat and waits for a fresh signal. Independently of that, Position protection follows every entry fill and closes at 1.6% profit or 0.8% loss from the entry price.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Reference Security | TONUSDT@BNBFT * 1 | Expression the Index block builds the reference security from. It names one instrument and multiplies it by one, which leaves the price untouched and keeps the setting an arithmetic expression. |
| Traded Candles | 00:15:00 | Time frame of the traded candle series, and the beat every order is built on. |
| Reference Candles | 00:15:00 | Time frame of the reference candle series. It has to match the traded one, or the two legs never fall into the same bar. |
| Sync Interval | 00:15:00 | Length of the bucket Sync groups the two legs into; the same length as the candles. |
| Traded MACD Fast | 12 | Fast moving average length of the MACD built on the traded instrument. |
| Traded MACD Slow | 26 | Slow moving average length of the same MACD. The gap between it and the fast length decides how long a move has to last before the histogram reacts. |
| Traded MACD Signal | 9 | Signal line length of the same MACD; it is the line the histogram is measured against. |
| Reference MACD Fast | 12 | Fast moving average length of the MACD built on the reference instrument. Each leg carries its own three lengths, so the two can be tuned apart, but a comparison of the two histograms only means something while they are set the same. |
| Reference MACD Slow | 26 | Slow moving average length of the reference MACD. Keep it equal to the traded one unless the two instruments are meant to be measured over different horizons. |
| Reference MACD Signal | 9 | Signal line length of the reference MACD. Keep it equal to the traded one for the same reason. |
| Order Volume | 1 | Order size, in lots. |
| Take Profit, % | 1.6 | Take-profit distance, in percent of the entry price. |
| Stop Loss, % | 0.8 | Stop-loss distance, in percent of the entry price. |

## Diagram Details

- Both candle blocks take finished candles only. An update of a forming candle carries the time the bar opened, and an order built from it would be older than the moment it is sent.
- Sync releases a set under the earliest time of the values in it, which is why nothing from Sync reaches an order directly: the four variables re-read the values on the traded candle, and it is that candle's beat the orders are built on. The cost is that a comparison uses the last completed pair of readings, one bar behind the candle it acts on.
- Every variable that holds a constant — zero and the order volume — is triggered by the traded candle. A variable without a trigger keeps its value and never emits it, and a condition waiting on it would never come together.
- Both MACD blocks emit only formed and final values, so the comparison begins once each leg has a complete indicator behind it and cannot move inside a bar.
- Both ends of every Sync line are linked. A value sent in and never taken out leaves the block waiting for it, and the strategy will not start.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
