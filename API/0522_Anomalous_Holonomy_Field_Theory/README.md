# Anomalous Holonomy Field Theory Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines EMA, RSI, MACD, ATR, rate of change and VWAP distance into a composite signal. Long positions are opened when the signal exceeds a user-defined threshold, while short positions are opened when it falls below the negative threshold. An ATR-based stop protects open trades.

## Details

- **Entry Criteria**:
  - **Long**: signal ≥ threshold.
  - **Short**: signal ≤ −threshold.
- **Long/Short**: Both.
- **Exit Criteria**: ATR stop.
- **Stops**: Yes, ATR-based.
- **Default Values**:
  - `SignalThreshold` = 2
  - `CandleType` = 5 minute
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
