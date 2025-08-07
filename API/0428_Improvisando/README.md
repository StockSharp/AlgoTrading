# Improvisando Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Improvisando mixes a basic EMA trend filter with RSI swings. The goal is to follow the prevailing direction indicated by the EMA while entering only when the RSI crosses the neutral 50 line. The original design also experimented with MACD style momentum but this simplified version focuses on clarity and ease of tuning.

The user can enable long and/or short trades separately.

## Details

- **Entry Criteria**:
  - **Long**: `Close > EMA` and `RSI > 50`
  - **Short**: `Close < EMA` and `RSI < 50`
- **Long/Short**: Configurable
- **Exit Criteria**:
  - Opposite signal
- **Stops**: None
- **Default Values**:
  - `EmaLength` = 10
  - `RsiLength` = 14
- **Filters**:
  - Category: Trend following
  - Direction: Configurable
  - Indicators: EMA, RSI
  - Stops: No
  - Complexity: Low
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
