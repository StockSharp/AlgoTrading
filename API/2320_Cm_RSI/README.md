# cm RSI Strategy

## Overview

This strategy is a direct port of the MetaTrader 4 expert "cm_RSI". It uses the Relative Strength Index (RSI) indicator to catch momentum reversals.

The algorithm monitors RSI values calculated from candle open prices. A long position is opened when RSI rises above a configurable *buy level* after being below it. A short position is opened when RSI falls below a configurable *sell level* after being above it. Each trade is protected by fixed take profit and stop loss values expressed in price points.

## Strategy Logic

1. Calculate the RSI with a user defined period using candle open prices.
2. If the previous RSI value was below the buy level and the current value crosses above it, open a long market position.
3. If the previous RSI value was above the sell level and the current value crosses below it, open a short market position.
4. Each trade uses the same configurable volume and is protected by both stop loss and take profit orders.

## Parameters

| Name | Description |
|------|-------------|
| `RsiPeriod` | RSI calculation period. |
| `BuyLevel` | RSI level used to trigger long entries. |
| `SellLevel` | RSI level used to trigger short entries. |
| `TakeProfit` | Take profit in absolute price points. |
| `StopLoss` | Stop loss in absolute price points. |
| `OrderVolume` | Volume applied to every trade. |
| `CandleType` | Type of candles used for calculations. |

## Notes

- The strategy processes only finished candles.
- It holds a single open position at any time.
- `StartProtection` is used to automatically manage stop loss and take profit orders.

