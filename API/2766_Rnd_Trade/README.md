# Rnd Trade Strategy

## Overview
- Conversion of the MetaTrader 5 expert advisor `RndTrade.mq5` to the StockSharp high-level strategy API.
- Closes any existing position on a fixed time interval and immediately opens a new market position in a randomly selected direction.
- Uses time-based candle subscriptions as a deterministic replacement for the original timer callbacks.

## Parameters
| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `IntervalMinutes` | `int` | `60` | Number of minutes between the close of the current position and the opening of a new random position. Must be greater than zero. |
| `Volume` | `decimal` | `1` | Position size used for market entries. Derived from the base `Strategy` class. |

## Data Subscriptions
- Subscribes to time frame candles whose length matches `IntervalMinutes` (e.g., `60` → 60-minute candles).
- The candle close event (`CandleStates.Finished`) is used to trigger the logic exactly once per interval.

## Trading Logic
1. Wait for the completion of each interval candle.
2. Skip processing until the strategy is formed, online, and trading is allowed.
3. Close any open position created during the previous interval.
4. Generate a random value to decide between a long or short entry.
5. Submit a market order (`BuyMarket` or `SellMarket`) with the configured volume in the selected direction.

## Implementation Notes
- Relies on `SubscribeCandles().Bind(ProcessCandle)` to avoid manual polling of indicator values or collections.
- Calls `StartProtection()` during startup so that the built-in risk module is active, even though no explicit stop loss or take profit is configured.
- Uses `Random` from the standard library to mirror the `MathRand()` behavior found in the original MQL strategy.
- The code contains English comments that explain how each conversion step maps to StockSharp features.

## Differences from the Original MQL Strategy
- Timer events (`OnTimer`) are emulated through candle subscriptions instead of MetaTrader's timer API.
- Position closing is handled with `ClosePosition()` rather than iterating over position lists and calling `PositionClose` for each ticket.
- The StockSharp version relies on the built-in `Volume` property for position sizing instead of the symbol's minimum lot query.
- Order filling rules and slippage settings are managed by the connected broker or simulator, so they are not explicitly configured in the strategy.

## Usage
1. Attach the strategy to a portfolio and security within the StockSharp environment.
2. Configure `IntervalMinutes` and `Volume` according to the desired trading frequency and size.
3. Start the strategy. It will automatically flatten and reopen positions at each interval without any additional input.
4. No Python implementation is provided at this time; only the C# version is available.
