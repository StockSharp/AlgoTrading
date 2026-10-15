# Pivot Limit Orders Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram turns classic pivot support and resistance into a persistent pair of limit orders. A rolling day of finished candles supplies the range, a short midnight window schedules the lifecycle, and the same live order references are carried through registration, replacement, cancellation, and charting.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed Highest and Lowest indicators with a 288-bar window, which represents one rolling day, while the latest close completes the pivot calculation.
- The formulas calculate P = (H + L + C) / 3, R1 = 2P - L, and S1 = 2P - H without chaining one formula through another.
- Working time emits the daily rebalance pulse around midnight. A state latch allows the first pulse with ready levels and a flat position to register exactly one buy/sell pair.
- Order registering posts a buy limit at S1 and a sell limit at R1. Price shrinking is disabled because the supplied level is already the intended order price.
- Combination keeps the newest order reference. Later daily pulses drive Order replacing, and each replacement is returned to the same bus so the following move acts on the live order.
- Position protection places a 1% stop after a fill. A protective fill triggers Order cancellation for any surviving pivot order, while an opposite pivot fill naturally nets an open position back toward flat.

## Entry and Exit Rules

- **Long entry**: After the 288-bar range is formed, the first eligible midnight pulse posts a buy limit at S1 with the configured volume. A touch of support fills the order; the sell order at R1 can then act as the opposing exit.
- **Short entry**: The same pulse posts a sell limit at R1. A touch of resistance opens the short, and the buy order at S1 provides the opposing limit exit.
- **Exit**: The opposite pivot order can flatten the position at the far side of the range. Independently, Position protection watches every pivot fill and closes adverse movement at 1%; its fill cancels any still-active buy or sell order.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 00:05:00 | Time frame of the finished candles used for the rolling-day range, pivot levels, order timing, and protection checks. |
| Order Volume | 1 | Quantity of each buy and sell limit order. |
| Stop Loss, % | 1 | Adverse distance from a pivot fill at which Position protection exits, in percent. |

## Diagram Details

- The 288-bar Highest/Lowest window is a rolling-day approximation. It avoids relying on a separate daily candle subscription and updates from the same finished stream that drives trading.
- R1 and S1 are expanded directly from H, L, and C. This preserves the pivot equations while ensuring both levels are produced on one processing layer.
- A flag-valued variable remembers that the initial pair has been armed. It prevents a true session condition from creating a fresh pair on every candle.
- Registered and replaced orders enter Combination<Order> buses. Replacement outputs loop back into those buses, so no later replacement holds a stale reference.
- All limit registrations and replacements use OnlineOnly = false and ShrinkPrice = false, which makes the lifecycle suitable for historical replay and preserves calculated prices.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
