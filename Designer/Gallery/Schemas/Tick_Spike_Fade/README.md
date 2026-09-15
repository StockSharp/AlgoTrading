# Tick Spike Fade Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A spike is not visible in a closing price. Price can run half a percent away from where it stood twenty minutes ago and be back before the minute is out, and the candle that records it looks like any other. This diagram watches the tape instead: every executed print is measured against the close twenty bars back, and the first print far enough away arms one side of the trade. The order then goes the other way — a rise is sold, a fall is bought — and it is sent on the close of the bar the spike appeared in.

![schema](schema.svg)

## Strategy Overview

- A one-minute candle series is the clock of the diagram. Only finished candles are sent on, so the reference price, the release timer and the moment an armed signal becomes an order are all counted in closed bars.
- The tape is subscribed alongside the candles, and a converter reads the price out of every executed print. That print price, not a candle close, is the current price of the whole diagram.
- A Previous value block keeps the candle twenty bars back and a converter takes its close. This is the reference the current price is measured against, far enough away that a single minute of noise cannot reach the threshold.
- A variable holds that reference and releases it on every print, so the formula after it has both inputs refreshed at the pace of the tape. It works out the distance between the last print and the reference close as a percentage, and it is guarded so a reference of zero produces no result at all.
- A second formula negates that percentage, which lets one threshold constant serve both directions: the comparison against it answers for a rise, the same comparison on the negated value answers for a fall.
- Each direction owns a flag. The first print that crosses the threshold sets its flag and the flag emits a single pulse; every later print of the same move is ignored, so one spike produces one decision rather than a hundred.
- That pulse arms an N-values block set to one value, which releases it on the next finished candle. The decision is taken between candles, on the tape, and the order is sent on a candle.
- Position modify opens by market under the open-position condition, so a diagram already holding something never adds to it. Position protection then owns the trade, and its Price socket is fed from the tape as well.

## Entry and Exit Rules

- **Long entry**: The last executed print is at or more than the threshold below the close twenty bars back, and the fall flag is free. The flag fires its single pulse, the one-value hold releases it on the close of the bar in progress, and Position modify buys the order volume at market.
- **Short entry**: The last executed print is at or more than the threshold above the close twenty bars back, and the rise flag is free. The flag fires its single pulse and, on the close of the bar the spike appeared in, Position modify sells the order volume at market.
- **Exit**: There is no exit signal and no closing rule of its own. Position protection takes the entry fill and sets a take-profit 0.6% and a stop-loss 0.3% away from the fill price; because its Price socket is fed from the tape, both levels are tested on every executed print instead of once a minute. Whichever is touched first closes the position with a market order, and the diagram is flat again well before the release timer runs out.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:01:00 | Time frame of the candle series. It is the clock of the diagram: the reference close, the release timer and the moment an armed signal becomes an order are all counted in these candles. |
| Lookback Bars | 20 | How far back the reference close is taken, in candles. A larger number measures the move over a longer window and makes the same threshold harder to reach. |
| Spike Threshold, % | 0.3 | How far the last print has to stand from the reference close, in percent, for the move to count as a spike. One value serves both directions. |
| Cooldown Bars | 30 | How many finished candles pass after a spike before the flags are released and the diagram may react again. |
| Order Volume | 1 | Order size, in lots. |
| Take Profit, % | 0.6 | Take-profit distance from the fill price, in percent, tested on every executed print. |
| Stop Loss, % | 0.3 | Stop-loss distance from the fill price, in percent, tested on every executed print. |

## Diagram Details

- Reading the current price from the tape is what turns the comparison into a spike detector. A move that runs a third of a percent away and comes back inside the same minute leaves almost no trace in the closing price, but every print of it passes through the comparison, and the first one across the line sets the flag.
- The reference is a close twenty bars back rather than the close before last. Over one minute even a violent move is small, and a threshold low enough to react to it would fire on ordinary noise.
- The flags are what make one spike one trade. A flag stays set until the release timer has counted thirty finished candles, so a move that keeps stretching cannot be sold three times on its way up.
- The order is timed by a candle although the decision was taken on a print: the one-value hold passes the armed signal to the first candle that finishes after it, and the entry order carries that candle's time.
- Both protective distances are percentages of the fill price rather than fixed steps, so the same figures mean the same thing on an instrument quoted near 65 000 and on one quoted near 5.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
