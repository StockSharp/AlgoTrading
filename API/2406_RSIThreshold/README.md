# RSI Threshold Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Converts the MetaTrader *Exp_RSI* expert into StockSharp. The strategy opens and closes positions when the Relative Strength Index (RSI) crosses predefined overbought and oversold levels.

## Details

- **Entry Criteria**:
  - **Long**: RSI crosses above `RSI Low Level`.
  - **Short**: RSI crosses below `RSI High Level`.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Reverse signal or stop parameters.
- **Stops**: Take Profit & Stop Loss in absolute price units.
- **Default Values**:
  - `RSI Period` = 14
  - `RSI High Level` = 60
  - `RSI Low Level` = 40
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Single
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: H4
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
