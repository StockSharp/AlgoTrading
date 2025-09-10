# Autonomous 5-Minute Robot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Autonomous 5-Minute Robot strategy trades on a 5-minute timeframe.
It goes long when price is trending up and buying pressure exceeds selling pressure,
and goes short on the opposite conditions.

## Details

- **Entry Criteria**: Uptrend (close above 50-period SMA and above the close 6 bars ago) with buy volume greater than sell volume.
- **Exit Criteria**: Reverse position on opposite signal.
- **Stops**: 3% stop loss and 29% take profit from entry price.
- **Default Values**:
  - `MaLength` = 50
  - `VolumeLength` = 10
  - `StopLossPercent` = 3
  - `TakeProfitPercent` = 29
  - `CandleType` = 5m
