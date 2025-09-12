# SMA RSI Volume ATR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a Simple Moving Average (SMA), Relative Strength Index (RSI), volume confirmation and an ATR-based volatility filter.
It buys when price is above the SMA, RSI is oversold, volume exceeds its moving average by a multiplier and volatility is rising. It sells under the opposite conditions.

Stops are managed with fixed percent take profit and stop loss levels.

## Details

- **Entry Criteria**:
  - **Long**: `Close > SMA` && `RSI < RsiOversold` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
  - **Short**: `Close < SMA` && `RSI > RsiOverbought` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or take-profit
- **Stops**: Yes, percent based
- **Default Values**:
  - `SmaLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `VolumeThreshold` = 1.5
  - `AtrLength` = 14
  - `TakeProfitPerc` = 1.5
  - `StopLossPerc` = 0.5
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, RSI, Volume, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
