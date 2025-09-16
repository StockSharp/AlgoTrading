# Parabolic SAR Bug5 Strategy

## Overview

Parabolic SAR Bug5 Strategy trades price reversals detected by the Parabolic SAR indicator. It opens a long position when price crosses above the SAR and a short position when price crosses below. The strategy optionally reverses trade direction, closes open positions on SAR flips, and supports trailing stop, take profit and stop loss rules.

## Entry Rules

- **Buy** when price crosses above the SAR and no long position is open.
- **Sell** when price crosses below the SAR and no short position is open.
- If `Reverse` is enabled the signals are inverted.

## Exit Rules

- Close position when opposite SAR signal appears if `SarClose` is enabled.
- Apply fixed stop loss and take profit targets.
- If `Trailing` is enabled the stop loss trails the highest (for longs) or lowest (for shorts) price since entry.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Step` | Initial acceleration factor for Parabolic SAR. |
| `Maximum` | Maximum acceleration factor for Parabolic SAR. |
| `StopLossPoints` | Stop loss distance in points. |
| `TakeProfitPoints` | Take profit distance in points. |
| `Trailing` | Enable trailing stop management. |
| `TrailPoints` | Trailing stop distance in points. |
| `Reverse` | Reverse trading direction. |
| `SarClose` | Close position on SAR switch. |
| `CandleType` | Timeframe of candles to process. |

