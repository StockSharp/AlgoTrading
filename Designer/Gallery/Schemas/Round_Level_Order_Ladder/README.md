# Round Level Order Ladder Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram manages the full life cycle of passive round-level entry orders. Finished five-minute candles drive a Kaufman adaptive average, directional crossings, calculated buy and sell levels, order replacement, timed cancellation, and an absolute trailing stop.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed KAMA(15) with fast period 2 and slow period 30; only formed indicator values continue through the signal path.
- A Previous value block shifts the formed KAMA by one update, and Crossing compares each close with that prior adaptive-average value.
- The nearest rounded center uses floor(close / step + 0.5). The buy limit is one 200-unit step below that center, and the sell limit is one step above it.
- Separate buy and sell order buses retain the currently active order. When its calculated level changes, Order replacing moves the active limit to the new level.
- Each side has its own twelve-candle cycle flag and timer. An opposite crossing or the timer cancels a still-active limit, while a fill arms trailing protection.

## Entry and Exit Rules

- **Long entry**: When the close crosses above the previous formed KAMA while the sampled position is zero and the buy-cycle flag is free, register a 0.1-unit buy limit at (floor(close / step + 0.5) - 1) × step.
- **Short entry**: When the close crosses below the previous formed KAMA while the sampled position is zero and the sell-cycle flag is free, register a 0.1-unit sell limit at (floor(close / step + 0.5) + 1) × step.
- **Exit**: A filled entry activates an absolute trailing stop with distance 10, evaluated from finished-candle closing prices and executed with a market order. Take-profit is disabled. Pending entries are canceled on the opposite crossing or after twelve finished candles.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles Series | 00:05:00 | Time frame of the finished candles used for signals, prices, timers, protection updates, and the chart. |
| KAMA Fast SC Period | 2 | Fast smoothing period of the Kaufman adaptive moving average. |
| KAMA Slow SC Period | 30 | Slow smoothing period of the Kaufman adaptive moving average. |
| KAMA Length | 15 | Lookback length of the Kaufman adaptive moving average. |
| KAMA Source | Not set | No alternate indicator input field is selected; candles are connected directly. |
| Round Level Step | 200 | Distance, in price units, between adjacent round levels. |
| Order Volume | 0.1 | Fixed volume of every registered and replaced entry limit. |
| Buy Order Life (N) | 12 | Number of finished candles in the buy-order cycle before its timed cancellation and flag reset. |
| Sell Order Life (N) | 12 | Number of finished candles in the sell-order cycle before its timed cancellation and flag reset. |
| Take Profit | 0 | A zero absolute value disables take-profit protection. |
| Stop Loss | 10 | Absolute trailing-stop distance from the best observed protected price. |
| Trailing Stop Loss | true | Moves the stop boundary when price advances in the position's favor. |
| Use Market Orders | true | Submits an activated trailing-stop exit as a market order. |

## Diagram Details

- The candle close reaches Crossing before the newly formed KAMA value is shifted. This gives Crossing one current close and one adaptive-average value from the preceding formed update.
- Both level formulas share the same close and step. Previous value blocks retain the prior buy and sell levels, and NotEqual comparisons emit a replacement pulse only when a level changes.
- Each Combination block accepts the order emitted by registration and every order emitted by replacement. Its output supplies the latest order to both Order replacing and Order cancellation.
- The N values blocks are armed by successful order registration and count twelve finished candles. Their outputs both request cancellation and release the corresponding cycle flag for a later setup.
- The chart shows five-minute candles, KAMA, both round levels, the current buy and sell limits, trailing-stop orders, and all fills.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
