# BTFD
[Русский](README_ru.md) | [中文](README_cn.md)

Volume and RSI based dip buying strategy with five take-profit levels and a protective stop.

## Details

- **Entry Criteria**: Volume spike over SMA and RSI below oversold.
- **Long/Short**: Long only.
- **Exit Criteria**: Five layered take-profit targets or stop loss.
- **Stops**: Yes.
- **Default Values**:
  - `VolumeLength` = 70
  - `VolumeMultiplier` = 2.5
  - `RsiLength` = 20
  - `RsiOversold` = 30
  - `Tp1` = 0.4
  - `Tp2` = 0.6
  - `Tp3` = 0.8
  - `Tp4` = 1.0
  - `Tp5` = 1.2
  - `Q1` = 20
  - `Q2` = 40
  - `Q3` = 60
  - `Q4` = 80
  - `Q5` = 100
  - `StopLossPercent` = 5
  - `CandleType` = TimeSpan.FromMinutes(3)
- **Filters**:
  - Category: Reversal
  - Direction: Long
  - Indicators: RSI, SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (3m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

