# QQE Signals Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implements the Quantitative Qualitative Estimation technique on RSI. The indicator builds dynamic upper and lower bands around a smoothed RSI line and tracks band crosses to signal trend changes. When RSI crosses above the trailing band a long signal is generated; crosses below trigger exits.

By adapting the bands to volatility, QQE seeks to smooth noise while remaining responsive. The strategy focuses on long trades and relies on the engine's trade reversals to close positions.

## Details

- **Entry Criteria**:
  - **Long**: RSI smoothed line crosses above the trailing band.
- **Exit Criteria**:
  - RSI falls below the opposite band or an opposite signal appears.
- **Indicators**:
  - RSI (period 14, smoothing 5)
  - QQE bands derived from ATR of RSI with factor 4.238
- **Stops**: None by default; relies on opposite signals.
- **Default Values**:
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.238
  - `Threshold` = 10
- **Filters**:
  - Trend-following
  - Single timeframe
  - Indicators: RSI, QQE
  - Stops: None
  - Complexity: Moderate
