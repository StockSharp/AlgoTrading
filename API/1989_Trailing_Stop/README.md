# Trailing Stop Strategy

## Overview

The **Trailing Stop Strategy** manages protective stop levels for existing positions. It does not generate entry signals; instead, it adjusts stops after a position is opened to lock in profits.

## Parameters

- `TrailingStop` – distance in price units that the market must move in favor of the position before a trailing stop is activated.
- `StopLoss` – optional initial stop-loss distance in price units. Set to `0` to disable.
- `CandleType` – type of candles used for price tracking.

## Trading Rules

1. When a position opens, an initial stop-loss is placed if `StopLoss` is greater than zero.
2. Once profit exceeds `TrailingStop`, the stop level trails the price, maintaining the specified distance.
3. The position is closed when price touches the trailing stop level.
4. The strategy works for both long and short positions.

## Notes

This strategy is designed to be used alongside another strategy that provides entry signals. It focuses solely on exit management through trailing stops.
