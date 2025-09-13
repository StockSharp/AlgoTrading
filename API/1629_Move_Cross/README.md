# Move Cross Strategy

## Overview

This strategy demonstrates a simplified conversion of the original `move_cross.mq4` script. It employs the RAVI (Range Action Verification Index) indicator calculated from two simple moving averages to determine trend direction.

The strategy compares hourly and daily RAVI values:

- **Buy** when the hourly RAVI is negative while the daily RAVI is positive and rising.
- **Sell** when the hourly RAVI is positive while the daily RAVI is negative and falling.

Positions are opened at market with optional profit target and stop loss.

## Parameters

| Name       | Description                          | Default |
|------------|--------------------------------------|---------|
| TakeProfit | Profit target in points               | 50      |
| StopLoss   | Loss limit in points                  | 100     |

## Notes

- The strategy uses two SMA pairs (2 and 24 periods) to compute RAVI on hourly and daily candles.
- It is intended for educational purposes and may require further tuning for live trading.
