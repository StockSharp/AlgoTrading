# Linear Cross Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy calculates a linear regression of price based on volume to produce a predicted price. A long position is opened when the predicted price crosses above its weighted moving average and the MACD line is rising above its signal. A short position is opened when the MACD line falls below its signal and recent lows are declining.

## Details

- **Entry Criteria**:
  - **Long**: Predicted price crosses above its WMA and MACD is rising above the signal.
  - **Short**: MACD is falling below the signal and lows are making lower lows.
- **Long/Short**: Both sides.
- **Exit Criteria**: None; positions are reversed on opposite signals.
- **Stops**: No.
- **Default Values**:
  - `Length` = 21.
  - `LinearLength` = 9.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Linear Regression, WMA, MACD
  - Stops: No
  - Complexity: Medium
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
