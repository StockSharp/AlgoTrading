# Trend Capture
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy combining Parabolic SAR with ADX filter. Long trades occur when price closes above the SAR value while ADX remains below a threshold, signalling a nascent trend. Short trades open on the opposite condition.

## Details

- **Entry Criteria**: Price above/below Parabolic SAR with ADX below `AdxLevel`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss, take profit or opposite signal.
- **Stops**: Fixed stop loss, take profit and break-even adjustment.
- **Default Values**:
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `AdxPeriod` = 14
  - `AdxLevel` = 20
  - `StopLoss` = 1800 points
  - `TakeProfit` = 500 points
  - `BreakEven` = 50 points
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Parabolic SAR, ADX
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
