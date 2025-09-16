# Darvas Boxes System Strategy

## Overview

This strategy implements a breakout approach based on the classic **Darvas Boxes** concept. It monitors price movement within a dynamic price range (box) calculated using the **Donchian Channels** indicator. When price closes above the upper boundary of the box, a long position is opened. When price closes below the lower boundary, a short position is opened. Optional stop-loss and take-profit levels provide basic risk management.

## How It Works

1. For each candle, the Donchian Channels indicator computes upper and lower boundaries using the specified `BoxPeriod`.
2. The strategy tracks the previous upper and lower values to detect breakouts.
3. If the current close price crosses above the previous upper boundary, the strategy:
   - Closes any existing short position (if allowed).
   - Opens a new long position (if allowed).
4. If the current close price crosses below the previous lower boundary, the strategy:
   - Closes any existing long position (if allowed).
   - Opens a new short position (if allowed).
5. Active positions are monitored for stop-loss and take-profit conditions.

## Parameters

- **BoxPeriod** (`int`): Number of candles used to build the price box. Default is 20.
- **StopLoss** (`decimal`): Distance from entry price to the stop-loss level. Default is 1000.
- **TakeProfit** (`decimal`): Distance from entry price to the take-profit level. Default is 2000.
- **AllowBuyEntry** (`bool`): Enables opening long positions. Default is `true`.
- **AllowSellEntry** (`bool`): Enables opening short positions. Default is `true`.
- **AllowBuyExit** (`bool`): Enables closing long positions on reverse signals or risk events. Default is `true`.
- **AllowSellExit** (`bool`): Enables closing short positions on reverse signals or risk events. Default is `true`.
- **CandleType** (`DataType`): Type of candles used for calculations. Default is 4-hour candles.

## Usage

1. Attach the strategy to a security and set desired parameter values.
2. Start the strategy. It will subscribe to the configured candle series and process incoming data.
3. Trades are executed using market orders when breakout conditions are met.
4. Optional stop-loss and take-profit levels manage open positions.

## Notes

- The strategy uses the high-level API with `BindEx` to connect indicator values and candle data.
- Internal collections are avoided; indicator values are accessed through the binding callback.
- Only finished candles are processed to ensure reliable signals.
- Comments inside the code are provided in English as required.

