# Step Stochastic Cross Strategy

## Overview
The strategy uses the Step Stochastic indicator (a custom oscillator based on ATR) to generate reversal signals. It subscribes to a user-selected candle timeframe and calculates fast and slow Step Stochastic lines scaled from 0 to 100.

## Entry and Exit Rules
- **Long Entry:** Slow line is above 50 and the fast line crosses from above to below the slow line.
- **Short Entry:** Slow line is below 50 and the fast line crosses from below to above the slow line.
- **Long Exit:** Slow line is below 50 and closing of long positions is allowed.
- **Short Exit:** Slow line is above 50 and closing of short positions is allowed.

## Parameters
- `KFast` – multiplier for fast channel.
- `KSlow` – multiplier for slow channel.
- `CandleType` – timeframe of candles.
- `AllowBuyOpen`, `AllowSellOpen`, `AllowBuyClose`, `AllowSellClose` – permissions for trade actions.
- `StopLoss`, `TakeProfit` – optional protective levels in price units.

The strategy calls `StartProtection` to apply stop-loss and take-profit when specified.

The `StepStochasticIndicator` is a C# port of the original MQL5 indicator and produces `Fast` and `Slow` values for each finished candle.
