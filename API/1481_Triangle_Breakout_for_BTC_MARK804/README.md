# Triangle Breakout Strategy for BTC
[Русский](README_ru.md) | [中文](README_cn.md)

Trades breakouts of simple moving average triangle when volume spikes and manages positions with ATR-based stops.

## Details

- **Entry Criteria**: price crossing above upper SMA line or below lower SMA line with volume above its SMA
- **Long/Short**: Both
- **Exit Criteria**: ATR-based stop-loss or take-profit
- **Stops**: Yes
- **Default Values**:
  - `TriangleLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrLength` = 14
  - `VolumeMultiplier` = 1.5
  - `AtrMultiplierSl` = 1.0
  - `AtrMultiplierTp` = 1.5
  - `CandleType` = 1 hour
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: SMA, ATR, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
