# PSAR Trader v2 Strategy

## Overview
This strategy trades market reversals using the Parabolic SAR indicator. A position is opened when the SAR value switches sides relative to price, signalling a potential change in trend. The algorithm operates only within a specified time window and can optionally close an existing position when an opposite signal appears.

## Strategy Logic
- **Indicator**: Parabolic SAR.
- **Buy** when SAR moves below the candle close after being above the previous candle.
- **Sell** when SAR moves above the candle close after being below the previous candle.
- Trades only during the `StartHour`–`EndHour` range.
- When `CloseOnOppositeSignal` is enabled, a position is closed if an opposite signal occurs before opening a new one.

### Risk Management
Upon entering a position the strategy sets internal take-profit and stop-loss levels. The position is closed automatically if price touches either level.

## Parameters
| Name | Description |
|------|-------------|
| `CandleType` | Timeframe of candles used for trading. |
| `Step` | Acceleration step of the Parabolic SAR. |
| `Maximum` | Maximum acceleration factor of the Parabolic SAR. |
| `TakeProfit` | Profit target in price units. |
| `StopLoss` | Stop loss in price units. |
| `StartHour` | Hour to start trading (0–23). |
| `EndHour` | Hour to stop trading (0–23). |
| `CloseOnOppositeSignal` | Close current position when an opposite signal appears. |

## Notes
This example demonstrates basic usage of the high level API with a popular trend reversal indicator. Adjust parameters and risk management according to the traded instrument and personal preferences.
