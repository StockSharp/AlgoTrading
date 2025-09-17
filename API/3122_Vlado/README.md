# Vlado Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Momentum reversal strategy based on the classic Larry Williams %R oscillator. The system waits for the oscillator to reach deep
 oversold or overbought readings and then reverses the position at the next completed bar. The StockSharp port keeps the discreti
tionary flavour of the original MetaTrader implementation while exposing every important setting as a parameter.

## Overview

- **Category**: Mean-reversion oscillator strategy.
- **Market**: Any liquid instrument that delivers stable candle data (forex pairs, index futures, crypto spot pairs).
- **Timeframe**: Configurable via `CandleType`. Defaults to 1-hour candles, matching the original usage example.
- **Direction**: Long and short. The engine always holds at most one position and flips when the opposite signal appears.
- **Indicator**: Williams %R with configurable lookback length and threshold levels.

## How It Works

1. Subscribes to the selected candle feed and calculates Williams %R on each finished candle.
2. Uses the default oversold level of -75 and overbought level of -25 (values are negative because of the oscillator scale).
3. When %R falls below the oversold level the strategy enters or reverses into a long position.
4. When %R rises above the overbought level the strategy enters or reverses into a short position.
5. Orders are sized with `Volume + Math.Abs(Position)` so a reversal closes the existing position and opens the new one in a singl
e market order.
6. No explicit stop-loss or take-profit is used. Risk is controlled by the indicator levels and chosen timeframe.
7. Every action is logged through `LogInfo`, making it easy to audit trades in the StockSharp GUI or log files.

## Parameters

- `WilliamsPeriod`: Number of candles used to compute the oscillator. Higher values smooth the signal, lower values react faster.
- `OverboughtLevel`: Threshold that defines when the market is considered overbought (default -25). Can be optimised.
- `OversoldLevel`: Threshold that defines when the market is considered oversold (default -75). Can be optimised.
- `CandleType`: Candle type and timeframe applied to all calculations. Works with time frames, volume candles, or range bars.
- `Volume` (inherited from `Strategy`): Defines the base order size. Adjust to match account size and risk appetite.

## Trading Rules

- **Long Entry**: Triggered when `%R <= OversoldLevel` and the current position is flat or short.
- **Short Entry**: Triggered when `%R >= OverboughtLevel` and the current position is flat or long.
- **Exit**: Performed implicitly by the reverse order when an opposite signal appears.
- **Position Management**: Always a single open position. The algorithm does not pyramid or scale out.

## Additional Notes

- Works best in range-bound or slow-trending markets where oscillators can cycle between extremes.
- Combining the strategy with external risk controls (equity stops, session filters) is recommended for live trading.
- The implementation includes chart rendering: the main area shows candles and trades, while a secondary pane plots Williams %R.
- Designed for further research: each parameter supports optimisation within StockSharp optimisers.
