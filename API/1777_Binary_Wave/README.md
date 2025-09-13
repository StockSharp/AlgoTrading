# Binary Wave Strategy

## Overview

Binary Wave Strategy combines several classic technical indicators into a single "binary" wave. Each indicator contributes either +1 or -1 depending on its bullish or bearish state. The weighted sum of all signals forms the final wave used for trading decisions.

## Parameters

- **Mode** – entry algorithm: `Breakdown` reacts to zero cross; `Twist` reacts to wave direction changes.
- **Candle Type** – timeframe of candles for all calculations.
- **Indicator Periods** – lengths for MA, MACD (fast, slow, signal), CCI, Momentum, RSI and ADX.
- **Weights** – contribution of each indicator to the wave. Setting a weight to 0 disables the indicator.
- **Trading Permissions** – enable or disable long/short entries and exits separately.
- **Risk** – stop-loss and take-profit in percent of entry price.

## How It Works

1. Subscribe to the specified candle series and calculate all indicators.
2. For each finished candle, evaluate the state of every indicator and convert it to a binary value (+1 / -1).
3. Sum weighted values to obtain the current wave.
4. Generate trading signals:
   - **Breakdown**: enter long when the wave crosses above zero, enter short when it crosses below zero.
   - **Twist**: enter long when the wave changes direction upwards, enter short when it turns downwards.
5. Optional protective stop-loss and take-profit are managed by the built-in position protection.

This approach allows flexible combination of multiple indicators while keeping the trading logic straightforward.
