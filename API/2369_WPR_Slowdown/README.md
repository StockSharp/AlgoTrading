# WPR Slowdown Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The WPR Slowdown strategy utilizes the Williams %R oscillator to detect reversals when momentum stalls near extreme levels. A slowdown occurs when the current Williams %R value differs from the previous value by less than one point. When such a slowdown appears above the upper threshold, the strategy closes short positions and optionally opens a long position. A slowdown below the lower threshold closes long positions and optionally opens a short position.

## Entry and Exit Rules
- **Long entry**: Williams %R is above `LevelMax` and the slowdown condition is satisfied. Short positions can be closed if allowed.
- **Short entry**: Williams %R is below `LevelMin` and the slowdown condition is satisfied. Long positions can be closed if allowed.
- **Long exit**: Triggered by a short entry signal when `BuyPosClose` is enabled.
- **Short exit**: Triggered by a long entry signal when `SellPosClose` is enabled.

## Parameters
- `WprPeriod` – period for calculating Williams %R.
- `LevelMax` – upper signal level (default -20) marking the overbought zone.
- `LevelMin` – lower signal level (default -80) marking the oversold zone.
- `SeekSlowdown` – enables slowdown detection between consecutive Williams %R values.
- `BuyPosOpen` – allow opening long positions.
- `SellPosOpen` – allow opening short positions.
- `BuyPosClose` – allow closing long positions on sell signals.
- `SellPosClose` – allow closing short positions on buy signals.
- `CandleType` – candle type used for indicator calculations (default 6-hour candles).

## Notes
The strategy focuses solely on the Williams %R slowdown logic from the original MQL5 expert. Alerting, money management and other auxiliary features are omitted for clarity. Stop-loss and take-profit functionality can be added manually if required.
