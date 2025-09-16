# Moving Average Trade System Strategy (2518)

## Overview

This strategy is a StockSharp port of the MetaTrader "Moving Average Trade System" expert advisor. It analyses the trend using four simple moving averages (SMA) calculated on the median candle price. The system waits for a confirmed crossover between the medium-term and long-term averages while the faster averages confirm trend alignment. Once confirmation arrives, the strategy flips its position in the direction of the new trend and manages risk with fixed take-profit, stop-loss, and trailing stop offsets defined in price steps.

## Trading Logic

1. **Indicators**
   - `SMA(5)` (fast) on median price.
   - `SMA(20)` (medium) on median price.
   - `SMA(40)` (signal) on median price.
   - `SMA(60)` (slow) on median price.

2. **Long Entry**
   - `SMA(5) > SMA(20) > SMA(40)`.
   - `SMA(40)` is above `SMA(60)` by at least `SlopeThresholdSteps` price steps.
   - `SMA(40)` crossed above `SMA(60)` on the current bar (previous `SMA(40)` was below or equal to the slow SMA).
   - If a short position is open, the strategy buys enough volume to close it and establish the desired long size.

3. **Short Entry**
   - `SMA(5) < SMA(20) < SMA(40)`.
   - `SMA(40)` is below `SMA(60)` by at least `SlopeThresholdSteps` price steps.
   - `SMA(40)` crossed below `SMA(60)` on the current bar (previous `SMA(40)` was above or equal to the slow SMA).
   - If a long position is open, the strategy sells enough volume to close it and establish the desired short size.

4. **Risk Management** (evaluated only when no new entry is triggered on the bar):
   - **Trend exit:** close longs when `SMA(40) <= SMA(60)` and close shorts when `SMA(40) >= SMA(60)`.
   - **Take profit:** exit once price reaches the configured take-profit distance from the entry price.
   - **Stop loss:** exit if price moves against the position by the configured stop-loss distance.
   - **Trailing stop:** once price advances beyond the entry, trail the protective stop by `TrailingStopSteps` price steps using the highest high (for longs) or the lowest low (for shorts) since entry.

All stop and profit offsets are measured in **price steps** (the instrument `PriceStep`). If the security does not report a price step, a value of `1` is used as a fallback.

## Parameters

| Name | Description | Default | Optimizable |
| --- | --- | --- | --- |
| `Volume` | Order volume used when opening new positions. | `1` | No |
| `TakeProfitSteps` | Distance to the take-profit target measured in price steps. | `50` | Yes |
| `StopLossSteps` | Distance to the protective stop measured in price steps. | `50` | Yes |
| `TrailingStopSteps` | Trailing-stop offset in price steps (`0` disables trailing). | `11` | Yes |
| `SlopeThresholdSteps` | Minimal separation between `SMA(40)` and `SMA(60)` to validate a breakout (in price steps). | `1` | Yes |
| `FastPeriod` | Length of the fast SMA. | `5` | Yes |
| `MediumPeriod` | Length of the medium SMA. | `20` | Yes |
| `SignalPeriod` | Length of the signal SMA (compared with the slow SMA). | `40` | Yes |
| `SlowPeriod` | Length of the slow SMA that defines the background trend. | `60` | Yes |
| `CandleType` | Candle series used for indicator calculations. | `1h time frame` | No |

## Implementation Notes

- Indicators are bound to the candle subscription through the high-level `Bind` API, ensuring calculations are event-driven and do not rely on manual buffer access.
- Median price is used for all SMA calculations, replicating the behaviour of the original MetaTrader EA.
- Position management stores the actual fill price using `OnNewMyTrade` in order to recalculate stop-loss, take-profit, and trailing-stop levels after every fill.
- When reversing a position, the strategy sends a single market order that both closes the existing exposure and opens the new one, mirroring the hedging-capable behaviour of the original algorithm.
- All comments inside the C# source file are written in English, as required by the repository guidelines.

## Usage Tips

- Configure the `Volume` parameter according to the instrument's lot size or contract multiplier.
- Adjust stop and profit distances to match the instrument's volatility (the defaults mirror the MetaTrader settings of 50 pips stop/take profit and an 11 pip trailing stop on FX pairs).
- The `SlopeThresholdSteps` parameter can be set to `0` to remove the additional spacing filter and react to any `SMA(40)`/`SMA(60)` crossover.
- For backtesting or live trading, ensure that the security provides a valid `PriceStep`; otherwise, the strategy will treat one price unit as a single step.
