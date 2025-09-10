# Bitcoin Bullish Percent Index Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Relative Strength Index (RSI) to approximate the Bitcoin Bullish Percent Index. It enters long when RSI rises above an oversold threshold and enters short when RSI falls below an overbought threshold.

## Details

- **Entry Criteria**:
  - **Long**: RSI crosses above the oversold level.
  - **Short**: RSI crosses below the overbought level.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `RSI Period` = 14
  - `Overbought` = 70
  - `Oversold` = 30
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RSI
  - Stops: No
  - Complexity: Low
  - Timeframe: Medium-term
