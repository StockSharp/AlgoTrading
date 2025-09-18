# ZeeZee Level Strategy

## Overview

The ZeeZee Level strategy replicates the behaviour of the original MetaTrader "ZeeZee Level" expert advisor using StockSharp's high level API. The strategy analyses ZigZag swings on the selected timeframe and trades in the direction of the most recent extreme. Protective stop loss, take profit and trailing stop distances are expressed in pips and the position size follows a martingale-style progression after losing trades.

## Trading Logic

1. Candles are subscribed using the timeframe defined by `CandleType`.
2. A `ZigZagIndicator` with configurable depth, deviation and backstep parameters tracks swing highs and lows.
3. When no position is open, the strategy compares the recency of the last confirmed ZigZag high and low within the `ZigZagIdInterval` window:
   - If the latest swing high is more recent than the last swing low, a short position is opened.
   - If the latest swing low is more recent than the last swing high, a long position is opened.
4. Only one position is maintained at a time. Entry volume is rounded to the instrument's volume step.
5. After the position is opened, stop loss, take profit and optional trailing stop levels are attached using the configured pip distances. The trailing stop follows the extreme price as the trade moves in its favour.
6. Positions are closed as soon as either the stop loss or the take profit level is touched. When both levels are reached in the same candle, the closer level to the entry price wins the tie.
7. After each exit the volume is reset to the initial value on profitable trades, or multiplied by the martingale factor on losing trades.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `ZigZagDepth` | Number of candles considered when searching for new ZigZag pivots. |
| `ZigZagDeviation` | Minimum price movement (in price steps) required to confirm a new pivot. |
| `ZigZagBackstep` | Minimum number of bars before the indicator can switch direction. |
| `ZigZagIdInterval` | Maximum number of bars used to look back for the last ZigZag highs and lows. |
| `StopLossPips` | Stop loss distance in pips. Set to zero to disable. |
| `TakeProfitPips` | Take profit distance in pips. Set to zero to disable. |
| `TrailingStopPips` | Trailing stop distance in pips. Set to zero to disable. |
| `InitialVolume` | Base trade volume used at the start of a martingale cycle. |
| `MartingaleMultiplier` | Factor applied to the next trade volume after a losing position. |
| `CandleType` | Candle type and timeframe used for the analysis. |

## Money Management

- Volumes are aligned with the instrument's volume step and constrained between the minimum and maximum exchange limits.
- Winning trades reset the volume to `InitialVolume` while losing trades multiply it by `MartingaleMultiplier`.

## Risk Management

- Stop loss, take profit and trailing stop distances are evaluated on every finished candle.
- The trailing stop moves only in the direction of the trade and never retreats.
- Trading is skipped while the strategy already holds a position or while the ZigZag swings are not available inside the configured interval.

## Notes

- The strategy uses only closed candles to match the behaviour of the original expert advisor.
- Pip conversions rely on the instrument's `PriceStep`. Ensure the instrument metadata is loaded before starting the strategy.
