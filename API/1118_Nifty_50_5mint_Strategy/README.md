# Nifty 50 5mint Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Nifty 50 5mint Strategy** trades breakouts on the Nifty 50 index using DEMA, VWAP and Bollinger Bands confirmation.

## Details
- **Entry Criteria**:
  - **Long**: close above previous high, close above upper Bollinger band and DEMA above VWAP.
  - **Short**: close below previous low, close below lower Bollinger band and DEMA below VWAP.
- **Long/Short**: both.
- **Exit Criteria**: stop-loss.
- **Stops**: yes, fixed points.
- **Default Values**:
  - `DemaPeriod = 6`
  - `BollingerLength = 20`
  - `BollingerStdDev = 2`
  - `LookbackPeriod = 5`
  - `StopLossPoints = 25`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: DEMA, VWAP, Bollinger Bands
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
