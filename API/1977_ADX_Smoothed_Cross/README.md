# ADX Smoothed Cross Strategy

## Summary

The strategy trades based on a double-smoothed Average Directional Index (ADX). It compares the smoothed +DI and -DI lines to detect trend changes. When the smoothed +DI line crosses above the smoothed -DI line, the strategy enters a long position. When the smoothed +DI line crosses below the smoothed -DI line, it opens a short position.

## How It Works

- Uses an ADX indicator with configurable period.
- Applies two exponential smoothing passes controlled by **Alpha1** and **Alpha2** parameters.
- A long signal occurs when the previous smoothed +DI was below the smoothed -DI and the current smoothed +DI is above.
- A short signal occurs on the opposite cross.
- Optional parameters allow disabling long or short trades and control whether existing positions can be closed when an opposite signal appears.
- Built-in risk management sets stop-loss and take-profit levels in points.

## Parameters

| Name | Description |
| ---- | ----------- |
| `AdxPeriod` | Period for the ADX calculation. |
| `Alpha1` | First smoothing coefficient (0-1). |
| `Alpha2` | Second smoothing coefficient (0-1). |
| `StopLoss` | Stop-loss in points. |
| `TakeProfit` | Take-profit in points. |
| `AllowBuy` | Enable long entries. |
| `AllowSell` | Enable short entries. |
| `AllowCloseBuy` | Allow closing long positions on sell signals. |
| `AllowCloseSell` | Allow closing short positions on buy signals. |
| `CandleType` | Timeframe used for the indicator. |

## Notes

- Only finished candles are processed.
- The strategy uses the high-level API with indicator binding.
- Stop-loss and take-profit are handled via `StartProtection`.
