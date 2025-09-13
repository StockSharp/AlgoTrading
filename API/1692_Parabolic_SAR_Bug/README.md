# Parabolic SAR Bug
[Русский](README_ru.md) | [中文](README_cn.md)

The **Parabolic SAR Bug** strategy trades trend reversals using the Parabolic SAR indicator. When the SAR flips below price the strategy enters long, and when the SAR flips above price it enters short. Optional reverse mode inverts signals. Protective stop loss, take profit, and trailing stop are supported through the built-in position protection module.

## Details

- **Entry Criteria**: Parabolic SAR direction change.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite SAR signal or protective stop.
- **Stops**: Stop loss, take profit, optional trailing stop.
- **Default Values**:
  - `Step` = 0.02
  - `MaxStep` = 0.2
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 1
  - `UseTrailingStop` = false
  - `Reverse` = false
  - `CloseOnSar` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Parabolic SAR
  - Stops: Stop loss, take profit
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
