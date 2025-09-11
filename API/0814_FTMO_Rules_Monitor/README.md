# FTMO Rules Monitor
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that tracks FTMO challenge rules and manages trades based on ATR risk.

The strategy sizes positions using ATR and stops when challenge objectives are met. It monitors maximum daily loss, total loss, profit target and minimum trading days.

## Details

- **Entry Criteria**: Bullish candle opens long, bearish candle opens short.
- **Long/Short**: Both directions.
- **Exit Criteria**: Challenge completed or opposite signal.
- **Stops**: ATR-based.
- **Default Values**:
  - `AccountSize` = 10000m
  - `RiskPercent` = 1m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Risk Management
  - Direction: Both
  - Indicators: ATR
  - Stops: ATR
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
