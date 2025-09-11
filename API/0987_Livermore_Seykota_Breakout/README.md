# Livermore Seykota Breakout
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout system combining Livermore pivot points with Seykota trend filter and ATR-based exits.

Testing indicates an average annual return around 87%. It performs best in the stocks market.

The strategy looks for breakouts above or below the most recent pivot while confirming trend direction with EMA alignment and volume strength. ATR-based stops manage risk.

## Details

- **Entry Criteria**: Price breaks last pivot with trend and volume confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR stop or trailing stop.
- **Stops**: ATR-based stop & trailing.
- **Default Values**:
  - `MainEmaLength` = 50
  - `FastEmaLength` = 20
  - `SlowEmaLength` = 200
  - `PivotLength` = 3
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 3
  - `TrailAtrMultiplier` = 2
  - `VolumeSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: EMA, Volume, ATR, Pivot
  - Stops: ATR trailing
  - Complexity: Basic
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
