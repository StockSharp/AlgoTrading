# Trailing Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This StockSharp implementation mirrors the MetaTrader expert advisor "Trailing_Profit". The strategy monitors the floating profit of all positions opened by the strategy and locks in gains whenever the profit retraces by a configurable percentage.

## Strategy Logic

1. The strategy subscribes to tick data and inspects profit at most once per configured interval (3 seconds by default) to avoid redundant calculations.
2. Portfolio equity is tracked as the sum of cash and open position value. The strategy stores the equity level when no positions are held and measures floating profit as the difference between the current equity and this baseline.
3. Once floating profit exceeds the activation threshold, a trailing stop is armed. The strategy remembers the peak profit and computes a trailing threshold equal to `peak_profit × (1 - trail_percent / 100)`.
4. Every time a new equity high is reached, the trailing threshold is recalculated upward, mirroring the original expert advisor behaviour.
5. If profit falls to or below the trailing threshold, the strategy closes every open position for the tracked instruments and reports the captured profit. The trailing state resets after all positions are flat.

This logic reproduces the MQL version where `minimum_profit` arms the trailing stop and `percent_of_profit` defines how much of the peak profit may be given back before forcing liquidation.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `PercentOfProfit` | 33 | Percentage of peak floating profit that may be surrendered before liquidation. |
| `MinimumProfit` | 1000 | Floating profit required to start trailing mode. |
| `CheckInterval` | 00:00:03 | Minimum delay between profit evaluations to throttle processing. |

All parameters can be optimized inside StockSharp Designer/Runner to fit a specific instrument or risk appetite.

## Implementation Notes

- Profit is derived from `Portfolio.CurrentValue` instead of iterating raw position objects. This provides a broker-agnostic way to measure unrealized gains, even when multiple orders are filled at different prices.
- `SubscribeTrades()` delivers tick updates similar to MetaTrader's `OnTick` function. The handler enforces the configurable interval so the trailing check runs at most once per period.
- `ClosePosition()` is called for the primary security, and any additional strategy positions are closed through the `Positions` collection to emulate the "close all" behaviour of the source expert advisor.
- The strategy starts protection services on launch so that standard StockSharp safeguards (such as kill-switch handling) remain active while profit management runs.

## Usage

Deploy the strategy together with any entry module that opens positions on the chosen instrument. Once the floating profit on those positions reaches the specified activation level, the trailing logic will protect gains automatically. After all positions close, the baseline equity resets and the strategy is ready for the next trading cycle.
