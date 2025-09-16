# FullDump BB RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A multi-step Bollinger Bands and RSI system converted from the MT5 "FullDump" expert advisor. The strategy waits for momentum exhaustion, confirms a mean-reversion bias with Bollinger Bands, and only trades when price realigns with the middle band. Trade management mirrors the original EA with fixed stop-loss/target offsets and a break-even adjustment when price returns to the opposite band.

## Overview

- **Markets**: Any liquid instrument that supports Bollinger Bands and RSI.
- **Timeframe**: Configurable candle type (default 15 minutes).
- **Direction**: Long and short.
- **Order Type**: Market orders with predefined protective levels.
- **Concept**: Fade short-term extremes inside the Bollinger envelope while price reverts toward the middle band.

## Trading Logic

1. **RSI scan (Step 1)**
   - Long condition requires at least one RSI reading below 30 within the recent window.
   - Short condition requires at least one RSI reading above 70 within the same lookback.
2. **Band violation (Step 2)**
   - Long: current close must be below or equal to any of the recent lower band values.
   - Short: current close must be above or equal to any of the recent upper band values.
3. **Middle band alignment (Step 3)**
   - Long trades only trigger once price closes back above the Bollinger middle line.
   - Short trades require the close to be below the middle line.
4. **Entry execution**
   - When all conditions match and no position is open in that direction, a market order is sent for the configured volume.

## Risk Management

- **Stop-loss**: Placed below (long) or above (short) the extreme low/high of the lookback window minus/plus the configured indent offset.
- **Take-profit**: Placed at the current opposite Bollinger band plus the same indent offset.
- **Break-even rule**: Once price touches the opposite band, the stop-loss is moved to the entry price to secure the position.
- **Position exit**: Positions close when price breaches the stop-loss or take-profit levels; opposite signals flatten the current position before flipping direction.

## Parameters

| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `BandsPeriod` | Length of the Bollinger Bands calculation. | 20 | Optimizable (10 → 40 step 1). |
| `RsiPeriod` | Averaging length for the RSI. | 14 | Optimizable (7 → 21 step 1). |
| `Depth` | Number of recent candles inspected for conditions. | 6 | Optimizable (3 → 12 step 1). |
| `IndentInPoints` | Offset in price steps added to stop-loss and take-profit. | 10 | Optimizable (5 → 30 step 5). |
| `OrderVolume` | Order size in lots. | 1 | Used for both entries and exits. |
| `CandleType` | Timeframe of the input candles. | 15-minute candles | Change to adapt the strategy horizon. |

## Filters & Tags

- **Category**: Mean reversion, volatility bands.
- **Indicators**: Bollinger Bands, Relative Strength Index.
- **Stops**: Hard stop, hard target, break-even adjustment.
- **Complexity**: Intermediate (multi-condition logic with stateful management).
- **Automation Level**: Fully automated entries and exits.
- **Best Use**: Range-bound phases where Bollinger extremes often revert toward the median.

## Notes

- The indent offset is scaled by the instrument price step to match the pip-based logic of the original EA.
- The algorithm keeps queues of the recent indicator values to replicate the MT5 depth checks exactly.
- Ensure the instrument provides enough historical candles to initialize both RSI and Bollinger Bands before live trading.
