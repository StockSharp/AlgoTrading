# US30 Stealth Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

Price action strategy for US30 using moving average slope, engulfing patterns, volume and session filter.
Position size is calculated from risk per trade, with stop-loss and take-profit based on candle range.

## Details

- **Entry Criteria**: Trend direction, three lower highs or higher lows, engulfing pattern, volume and time filter.
- **Long/Short**: Both
- **Exit Criteria**: Take-profit or stop-loss
- **Stops**: Fixed
- **Default Values**:
  - `MaLen` = 50
  - `VolMaLen` = 20
  - `HlLookback` = 5
  - `RrRatio` = 2.2
  - `MaxCandleSize` = 30
  - `PipValue` = 1
  - `RiskAmount` = 50
  - `LargeCandleThreshold` = 25
  - `MaSlopeLen` = 3
  - `MinSlope` = 0.1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Price action
  - Direction: Both
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
