# Linear On MACD
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining MACD signals on price and volume with linear regression.

## Details

- **Entry Criteria**: long when both MACDs are above signals and regression price sits between open and close; short on reverse.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Lookback` = 21
  - `RiskHigh` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD, Linear Regression
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Variable
