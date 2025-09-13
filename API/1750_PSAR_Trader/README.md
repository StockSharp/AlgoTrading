# PSAR Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The PSAR Trader strategy acts on shifts in the Parabolic SAR indicator. When the SAR flips below price a long position is opened, and when the SAR flips above price a short position is opened. An optional "Close On Opposite" setting reverses the position when an opposite signal appears. Trading occurs only during the configured session hours. Stop loss and take profit are managed by the protection module.

## Details

- **Entry Criteria**: Price crossing the Parabolic SAR.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite SAR crossing or position reversal.
- **Stops**: Yes, fixed via parameters.
- **Default Values**:
  - `SarStep` = 0.001m
  - `SarMaxStep` = 0.2m
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `TakeValue` = 50 (absolute)
  - `StopValue` = 50 (absolute)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Parabolic SAR
  - Stops: Fixed
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
