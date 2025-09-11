# Gold Breakout RR4 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Gold Breakout RR4 trades Donchian Channel breakouts on gold with volume and LWTI trend filters. Only one trade per day within a specified session and uses a fixed 4:1 risk/reward.

## Details

- **Entry Criteria**: price breaks Donchian channel with volume above average and LWTI confirmation within session
- **Long/Short**: Both
- **Exit Criteria**: fixed risk/reward stop and target
- **Stops**: Yes
- **Default Values**:
  - `DonchianLength` = 96
  - `MaVolumeLength` = 30
  - `LwtiLength` = 25
  - `LwtiSmooth` = 5
  - `StartHour` = 20
  - `EndHour` = 8
  - `RiskReward` = 4
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Donchian Channel, SMA, WMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
