# Fxscalper Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Bollinger Band breakout scalping strategy translated from the MQL4 "fxscalper" expert.
The strategy subscribes to candle data and Bollinger Bands. When the closing price breaks above the upper band it opens a long position; when the closing price breaks below the lower band it opens a short position. Positions are protected by stop-loss and take-profit levels.

## Details

- **Entry Criteria**:
  - Long: `Close > Upper Band`
  - Short: `Close < Lower Band`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal or protective stops
- **Stops**: Stop loss and take profit
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2
  - `StopLoss` = 200m
  - `TakeProfit` = 150m
- **Filters**:
  - Category: Bollinger Bands
  - Direction: Both
  - Indicators: Bollinger Bands
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
