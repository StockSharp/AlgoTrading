# Hancock RSI Volume Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy calculates a volume-weighted Relative Strength Index (RSI) inspired by the Hancock script from TradingView. The RSI uses bullish and bearish volume to gauge trend strength. A long position is opened when the RSI trend turns up, and a short position is opened when it turns down.

## Details

- **Entry Criteria**:
  - **Long**: RSI trend switches to up.
  - **Short**: RSI trend switches to down.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite trend signal.
- **Stops**: None.
- **Default Values**:
  - `RSI Length` = 14.
  - `Threshold` = 0.1.
  - `Use Wicks` = true.
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: RSI, Volume
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
