# RSI 30-70 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This simple momentum strategy uses the Relative Strength Index (RSI) to identify oversold and overbought zones. When RSI dips below the oversold level, a long position is opened. The trade is closed once RSI rises above the overbought threshold. The system operates on a single timeframe and does not take short trades.

## Details

- **Entry Criteria**:
  - **Long**: `RSI < oversold`.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - **Long**: `RSI > overbought`.
- **Stops**: None.
- **Default Values**:
  - `RSI Length` = 14.
  - `Overbought/Oversold` = 70 / 30.
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: Single
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
