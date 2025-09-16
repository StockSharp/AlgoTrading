# Catcher Profit 1.0 Strategy

## Overview
Catcher Profit 1.0 monitors the account profit and immediately closes all open positions once a preset target is reached. The target can be defined as an absolute currency amount or as a percentage of the starting balance.

A candle subscription is used only to trigger periodic checks on finished candles. The candle data itself is not used for calculations.

## Parameters
- **Candle Type**: Timeframe of candles used to trigger profit checks.
- **Maximum Profit**: Fixed profit in account currency required to close all positions.
- **Use Percentage**: Enables the percentage-based target when set to `true`.
- **Maximum Percentage**: Percentage of the initial balance required to close positions.

## How It Works
1. When the strategy starts it stores the current portfolio value as the initial balance.
2. On each finished candle the strategy calculates the current profit.
3. If there is an open position and the profit exceeds the configured absolute or percentage limit, all active orders are canceled and the position is closed with a market order.

This approach allows locking in gains automatically without relying on manual intervention.
