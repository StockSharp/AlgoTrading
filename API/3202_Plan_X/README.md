# Plan X Strategy

## Overview

The Plan X strategy is a breakout system converted from the original MetaTrader 5 expert advisor. It evaluates the close of each finished candle against a reference candle shifted by a configurable number of bars. When the most recent close exceeds the reference close by a specified channel height, the strategy opens a position in the breakout direction. Optional signal reversal allows trading breakouts in the opposite direction.

The implementation uses StockSharp's high-level API. It supports adjustable stop-loss, take-profit, trailing stop logic and a trading session filter.

## How it works

1. **Candle processing** – the strategy subscribes to the configured candle type and processes only finished candles. A short history of closes is maintained to compare the latest value with a shifted reference bar.
2. **Breakout detection** – if the latest close is higher than the reference close by more than the channel height, a long signal is produced. If it is lower by the same amount, a short signal is generated. When the reversal flag is enabled, the signals are flipped.
3. **Order execution** – the strategy uses market orders. When reversing from an opposite position, the order volume automatically includes the absolute value of the current position to flatten and re-enter in a single operation.
4. **Risk management** – stop-loss and take-profit levels are set immediately after entry. A trailing stop can replace the original stop when price moves favorably by more than the trailing distance plus the trailing step.
5. **Time filter** – trading can be limited to a start and end hour. If the start hour is greater than the end hour, the window is treated as crossing midnight.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Stop Loss (pips)` | Protective stop distance in pips, converted to price units based on the security price step. |
| `Take Profit (pips)` | Target distance in pips. |
| `Trailing Stop (pips)` | Distance between price and the trailing stop. Set to zero to disable trailing. |
| `Trailing Step (pips)` | Additional profit required before the trailing stop is advanced. Must be positive when trailing is enabled. |
| `Channel Height (pips)` | Breakout threshold expressed in pips. |
| `Candle Shift` | Number of bars between the latest close and the reference candle. |
| `Use Time Control` | Enables or disables the trading session filter. |
| `Start Hour` | First hour (0–23) when trading is allowed. |
| `End Hour` | Final hour (0–23) when trading is allowed. |
| `Reverse Signals` | Flips the breakout direction. |
| `Order Volume` | Market order size expressed in lots/contracts. |
| `Candle Type` | Data type of candles used for analysis. |

## Signal logic

- **Long entry** – latest close ≥ reference close + channel height, reversal disabled.
- **Short entry** – latest close ≤ reference close − channel height, reversal disabled.
- When reversal is enabled, the logic swaps the long and short conditions.

## Trailing stop logic

- The trailing stop activates when the favorable move exceeds `Trailing Stop + Trailing Step` in price terms.
- For long positions the stop is moved to `high − Trailing Stop` if the new value is higher than the existing stop.
- For short positions the stop is moved to `low + Trailing Stop` if the new value is lower than the existing stop.

## Additional notes

- The pip size calculation emulates the MQL version by multiplying the price step by 10 for 3- or 5-decimal instruments.
- Trading outside the permitted session skips new entries but still manages open positions.
- The strategy calls `StartProtection()` once during startup to enable built-in portfolio protection services.
