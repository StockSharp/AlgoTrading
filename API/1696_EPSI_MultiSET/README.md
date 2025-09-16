# EPSI Multi SET Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy converted from the original MQL4 expert *e-PSI@MultiSET*.
It watches each candle and enters when price moves a specified distance from the open.
Positions use take-profit and stop-loss levels and trades are only allowed during a
user-defined time window.

## Details

- **Entry Criteria**:
  - Long: `High - Open >= MinDistance`
  - Short: `Open - Low >= MinDistance`
- **Long/Short**: Both
- **Exit Criteria**: TakeProfit or StopLoss
- **Stops**: Yes
- **Default Values**:
  - `MinDistance` = 20
  - `TakeProfit` = 20
  - `StopLoss` = 200
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
  - `OpenHour` = 2
  - `CloseHour` = 20
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
