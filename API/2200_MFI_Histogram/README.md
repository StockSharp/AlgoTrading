# MFI Histogram Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The MFI Histogram Strategy uses the Money Flow Index (MFI) to detect overbought and oversold conditions via configurable thresholds. The MFI combines price and volume to measure the intensity of capital inflow and outflow. When the indicator crosses above the high level from below, the strategy interprets this as rising buying pressure and enters a long position while closing any existing short. Conversely, a cross below the low level triggers a short entry and closes existing longs. Stop-loss and take-profit values are managed in ticks through the built-in protection mechanism.

The strategy operates on a user-defined candle timeframe (4 hours by default) and relies on a single indicator without additional filters. Parameters allow optimization of the MFI period, threshold levels, and risk limits, making the system adaptable to various markets and volatility regimes.

## Details

- **Entry Criteria**:
  - **Long**: `MFI` crosses above `HighLevel` from below.
  - **Short**: `MFI` crosses below `LowLevel` from above.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal generates a reversal.
  - Stop-loss or take-profit is reached.
- **Stops**: `StopLoss` and `TakeProfit` in ticks.
- **Default Values**:
  - `MFI Period` = 14
  - `HighLevel` = 60
  - `LowLevel` = 40
  - `Candle Type` = 4-hour
  - `StopLoss` = 1000 ticks
  - `TakeProfit` = 2000 ticks
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Single
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
