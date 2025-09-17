# Bull Row Breakout Strategy

## Overview
The Bull Row Breakout strategy is a C# conversion of the MetaTrader 5 expert advisor "BULL row full EA". The original robot was built with a block-constructor and combines price action patterns with momentum confirmation. The StockSharp port reproduces the same logic on a single configurable timeframe and keeps the trading commentary in English as required.

The strategy opens **long-only** positions after a sequence of bearish candles is followed by bullish momentum and a breakout above recent highs. Stochastic Oscillator filters control the momentum strength while dynamic stop loss and target levels recreate the risk settings from the MQL version.

## Signal Logic
1. Wait for a new candle to close ("once per bar" execution).
2. Verify that no long position is currently open.
3. Detect a bearish row:
   - `BearRowSize` consecutive candles starting at `BearShift` bars back must be bearish.
   - Each candle body must be at least `BearMinBody` price steps.
   - Body progression must satisfy the selected `BearRowMode` (normal / bigger / smaller).
4. Detect a bullish row:
   - `BullRowSize` consecutive candles starting at `BullShift` bars back must be bullish.
   - Each candle body must be at least `BullMinBody` price steps.
   - Body progression must satisfy `BullRowMode`.
5. Breakout confirmation: the close of the latest finished candle must be higher than the highest high recorded from bar 2 up to `BreakoutLookback` bars ago.
6. Stochastic confirmation:
   - Current %K (`StochasticKPeriod`) must be above %D (`StochasticDPeriod`).
   - The last `StochasticRangePeriod` %K values must stay between `StochasticLowerLevel` and `StochasticUpperLevel`.
7. Risk management:
   - Stop price is the lowest low among the last `StopLossLookback` candles (starting from the latest closed bar).
   - Take profit is placed at a distance equal to `TakeProfitPercent` percent of the stop distance.
   - The stop and target are monitored on every closed candle; if either level is reached intrabar, the position is closed at market on the next update.

## Parameters
| Parameter | Description |
| --- | --- |
| `Volume` | Fixed trade volume used for each entry. |
| `CandleTimeFrame` | Timeframe of the processed candles. |
| `StopLossLookback` | Number of bars used to calculate the dynamic stop price. |
| `TakeProfitPercent` | Reward distance expressed as a percentage of the stop distance. |
| `BearRowSize`, `BearMinBody`, `BearRowMode`, `BearShift` | Configuration of the bearish row that precedes the breakout. |
| `BullRowSize`, `BullMinBody`, `BullRowMode`, `BullShift` | Configuration of the bullish row that immediately precedes the signal. |
| `BreakoutLookback` | Length of the rolling high used for breakout confirmation. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Stochastic Oscillator settings. |
| `StochasticRangePeriod` | Number of historical Stochastic values that must stay inside the bounds. |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Oscillator channel limits applied to %K. |

All body sizes are expressed in price steps to mirror the `toDigits` helper from the original code. When the instrument does not provide a price step, a default value of 1 is used.

## Differences from the MQL Version
- The MT5 project allowed separate timeframes for the block inputs. The StockSharp port operates on one timeframe defined by `CandleTimeFrame`, matching the common usage of the original EA (all blocks at chart timeframe).
- Virtual stops and pending order handling from the generic block library are not required and therefore omitted.
- Protective stop-loss and take-profit levels are emulated by monitoring candles and closing the position with `SellMarket` once a level is breached.
- Logging and chart decorations from the MQL environment are not replicated.

## Usage Tips
- Optimise the row sizes and shifts for the traded instrument. The default values mimic the original preset (three bearish candles starting three bars back followed by two bullish candles starting one bar back).
- Adjust `StochasticLowerLevel` and `StochasticUpperLevel` to tune how restrictive the oscillator filter should be.
- Because the stop is based on recent lows, instruments with large gaps may require widening the lookback or adding additional filters.
