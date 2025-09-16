# Diff TF MA Strategy

## Overview
- This strategy is a StockSharp port of the MetaTrader "Diff_TF_MA_EA" expert advisor.
- Trading signals come from comparing a simple moving average calculated on a higher timeframe with another moving average that is rescaled to the trading timeframe.
- The code keeps only finished candles, mirrors the original crossover rules, and closes any opposite exposure before opening a new position.

## Parameters
| Name | Description |
| --- | --- |
| `MaPeriod` | Length of the simple moving average calculated on the higher timeframe. |
| `CandleType` | Trading timeframe used for order generation. |
| `HigherCandleType` | Higher timeframe that supplies the reference moving average. |
| `ReverseSignals` | Inverts the crossover rules (buy on bearish cross and sell on bullish cross). |
| `Volume` | Strategy volume used by `BuyMarket`/`SellMarket` calls (set through the base `Strategy.Volume` property). |

## Trading logic
1. Subscribe to both the trading timeframe (`CandleType`) and the higher timeframe (`HigherCandleType`).
2. Build a simple moving average with length `MaPeriod` on the higher timeframe.
3. Convert the higher timeframe length into the trading timeframe by multiplying by the ratio of timeframe durations and run another moving average on the trading candles.
4. Store the last two completed values for both moving averages and check for crossings on every finished trading candle.
5. Open or reverse to a long position when the higher timeframe MA crosses above the trading MA (unless `ReverseSignals` is `true`).
6. Open or reverse to a short position when the higher timeframe MA crosses below the trading MA (unless `ReverseSignals` is `true`).
7. Positions are flattened and flipped by sending enough volume to offset any existing exposure.

## Usage notes
- Choose compatible timeframes: the higher timeframe should usually be larger than the trading timeframe so the rescaled length is meaningful.
- The default volume is `1`. Adjust `Strategy.Volume` before starting the strategy if another size is required.
- Stops and take-profits from the MetaTrader version are not reproduced; risk management can be attached through StockSharp protections if needed.
- When `ReverseSignals` is enabled, bullish and bearish actions are swapped while the rest of the logic remains unchanged.
