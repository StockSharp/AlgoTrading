[Русский](README_ru.md) | [中文](README_cn.md)

The I4 DRF v2 strategy applies the custom i4_DRF_v2 indicator that counts the number of up and down closes over a sliding window.
Depending on the TrendMode parameter it can work in contrarian (Direct) or trend-following (NotDirect) mode.
The strategy opens and closes positions when the indicator changes its sign and supports optional stop loss and take profit in price steps.

## Details

- **Entry Criteria**: Indicator sign flip according to TrendMode
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal or stop loss/take profit
- **Stops**: Yes
- **Default Values**:
  - `Period` = 11
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `TrendMode` = Direct
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Custom
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
