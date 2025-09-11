# MACD Volume XAUUSD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

15-minute strategy for XAUUSD combining MACD zero-line cross with a volume oscillator filter and fixed risk parameters.

## Details

- **Entry Criteria**: MACD crossing the zero line with positive volume oscillator and volume comparison.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop-loss or take-profit levels.
- **Stops**: Fixed stop-loss and take-profit multiplier.
- **Default Values**:
  - `ShortLength` = 5
  - `LongLength` = 8
  - `FastLength` = 16
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Leverage` = 1.0
  - `StopLoss` = 10100
  - `TakeProfitMultiplier` = 1.1
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: MACD, EMA, Volume
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
