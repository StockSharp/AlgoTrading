# RMI Trend Sync
[Русский](README_ru.md) | [中文](README_cn.md)

RMI Trend Sync blends RSI and MFI momentum signals with a SuperTrend trailing stop. A long trade opens when average momentum crosses above a threshold with rising EMA slope, while a short trade triggers on a downward break. SuperTrend provides the exit trail.

## Details

- **Entry Criteria**: Momentum average crosses thresholds with EMA slope confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite momentum or SuperTrend stop.
- **Stops**: Yes.
- **Default Values**:
  - `RmiLength` = 21
  - `PositiveThreshold` = 70
  - `NegativeThreshold` = 30
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3.5
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RSI, MFI, EMA, SuperTrend
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
