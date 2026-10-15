# Fibonacci SAR Pullback Limit
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram combines two Parabolic SAR speeds with a three-candle range, places one Fibonacci pullback limit at a time, cancels a resting limit when its setup reverses, and exits a filled position at range-derived levels saved with the entry.

![schema](schema.svg)

## Strategy Overview

- Finished one-hour BTCUSDT candles feed fast and slow Parabolic SAR plus Highest(3) and Lowest(3). Decisions begin only after every indicator is formed.
- The formed Lowest output releases one decision batch after the current Close, SAR, high, low, position, and pending-order state have all been captured.
- A global pending lock permits only one live entry order. It clears when that order reaches its final state, while the sampled position prevents another entry after a fill.
- Entry, cancellation, and exit predicates are evaluated from silent score latches and released once per finished candle, avoiding mixed values from adjacent candles.
- The chart shows candles, both SAR series, range and saved protection levels, registered and cancelled limits, market exits, and all fills.

## Entry and Exit Rules

- **Long entry**: When `Slow SAR < Fast SAR < Close`, the position is flat, and no entry is pending, submit a Buy limit at `Low3 + (High3 - Low3) * 50%`. Cancel it if `Slow SAR > Fast SAR` or `Fast SAR >= Close` before it fills.
- **Short entry**: When `Slow SAR > Fast SAR > Close`, the position is flat, and no entry is pending, submit a Sell limit at `High3 - (High3 - Low3) * 50%`. Cancel it if `Slow SAR < Fast SAR` or `Fast SAR <= Close` before it fills.
- **Exit**: When an entry setup is accepted, save the side-specific levels. A long uses stop `Low3 - 30` and target `Low3 + (High3 - Low3) * 161%`; a short uses stop `High3 + 30` and target `High3 - (High3 - Low3) * 161%`. A finished Close reaching either saved level triggers one opposite market order of Volume 1.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Security | BTCUSDT@BNBFT | Instrument used by the finished candle subscription. Set Strategy Security to the same instrument for orders and fills. |
| Candle Series | 01:00:00 | Finished one-hour candles used for indicators, decisions, exits, and the chart. |
| Fast SAR Acceleration | 0.02 | Initial acceleration of the fast Parabolic SAR. |
| Fast SAR Increment | 0.02 | Acceleration increment of the fast Parabolic SAR. |
| Fast SAR Maximum | 0.20 | Maximum acceleration of the fast Parabolic SAR. |
| Slow SAR Acceleration | 0.01 | Initial acceleration of the slow Parabolic SAR. |
| Slow SAR Increment | 0.02 | Acceleration increment of the slow Parabolic SAR. |
| Slow SAR Maximum | 0.10 | Maximum acceleration of the slow Parabolic SAR. |
| High Lookback | 3 | Number of finished candles used by Highest for `High3`. |
| Low Lookback | 3 | Number of finished candles used by Lowest for `Low3`. |
| Entry Fibonacci, % | 50 | Position of the limit price inside the current three-candle range. |
| Target Fibonacci, % | 161 | Range multiplier used for each saved profit target. |
| Stop Offset | 30 | Absolute price distance beyond the three-candle low or high used for the saved stop. |
| Order Volume | 1 | Quantity of each entry and each side-guarded market exit. |

## Diagram Details

- The Security [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) configures finished [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html); transaction blocks use Strategy Security and Strategy Portfolio.
- Four formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks calculate both Parabolic SAR values and the separate three-candle high and low. The lowest-range output is the shared batch clock.
- [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html), Variable, and [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks keep numeric inputs aligned, apply flat and pending-side guards, and emit only true action pulses.
- Each accepted setup stores its calculated stop and target before its [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) block is triggered. The saved values do not move while the position is open.
- The registered Order reference is retained for addressed [Order cancellation](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html). The pending lock clears only from the registration block's Finished event after a fill, confirmed cancellation, or registration failure.
- Side-guarded [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks submit a fixed one-unit opposite market order when the finished Close reaches a saved stop or target. The chart receives every relevant price, order, cancellation, and MyTrade stream.

## Usage

Import the `.json` file into Designer, set Strategy Security to BTCUSDT@BNBFT, and run it on one-hour history. Review the instrument's price scale, Fibonacci levels, stop offset, order lifecycle, and market-exit behavior before using the diagram in live trading.
