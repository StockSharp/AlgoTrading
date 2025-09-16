# Renko Level EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
- Converted from the MetaTrader expert advisor **Renko Level EA.mq5**.
- Emulates the original indicator by maintaining an upper and lower Renko level derived from the `BrickSize` parameter.
- Evaluates finished candles provided by `CandleType` (default: 1-minute timeframe) and reacts when the Renko grid shifts.
- Does not use fixed stops or targets; every exit happens through an opposite signal.

## Trading Logic
1. On the first finished candle the close price is rounded to the Renko grid to initialize upper and lower levels.
2. For each subsequent candle:
   - If the close remains between the current bounds, the grid stays unchanged.
   - A close above the upper level lifts the Renko block upward to the next grid value.
   - A close below the lower level pushes the block downward.
3. A change in the upper Renko level is interpreted as a directional breakout.
   - Rising upper level → bullish signal (unless `ReverseSignals` is enabled).
   - Falling upper level → bearish signal.
4. Signals can optionally be flipped (`ReverseSignals`) or pyramided (`AllowIncrease`) to match the original EA behaviour.

## Order Management
- Before entering long, any short position is closed; the opposite happens before entering short.
- When `AllowIncrease = false`, the strategy opens a new trade only if no position already exists in that direction.
- When `AllowIncrease = true`, additional orders of size `OrderVolume` are allowed even if a position is already open.
- There is no dedicated stop-loss or take-profit; position reversals serve as the exit mechanism.
- `StartProtection()` is invoked once to keep risk safeguards aligned with the base framework.

## Parameters
| Name | Description | Default | Optimizable |
| --- | --- | --- | --- |
| `BrickSize` | Renko block size measured as multiples of `Security.PriceStep`. Defines how far price must move to shift the grid. | `30` | Yes (10 → 100 step 10) |
| `OrderVolume` | Volume submitted with each market order. | `1` | No |
| `ReverseSignals` | Inverts bullish and bearish actions. Mirrors the EA's *Reverse* input. | `false` | No |
| `AllowIncrease` | Permits adding to an existing position instead of waiting for a flat position. Mirrors the EA's *Increase* flag. | `false` | No |
| `CandleType` | Candle source used for the calculations. Defaults to 1-minute time-frame candles, but any supported series can be supplied. | `TimeFrameCandleMessage(1m)` | No |

## Practical Notes
- `BrickSize` adapts automatically to the traded instrument because it multiplies the exchange-defined `PriceStep`.
- The decision is based purely on closing prices; intrabar movements matter only when they form the final close.
- Combining `ReverseSignals` and `AllowIncrease` allows testing both counter-trend and pyramiding variants of the EA.
- Works on any market where Renko-style breakout logic is relevant, including forex, futures, and crypto instruments.

## Classification
- **Regime**: Trend following (Renko breakout).
- **Direction**: Long & Short.
- **Complexity**: Moderate (custom level tracking, minimal tuning).
- **Stops**: None; exits on reverse signals.
- **Timeframe**: Configurable via `CandleType`.
- **Indicators**: Custom Renko level projection.
