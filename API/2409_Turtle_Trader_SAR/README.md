[Русский](README_ru.md) | [中文](README_cn.md)

Turtle Trader SAR converts the original MQL5 Turtle system with an optional Parabolic SAR trail into StockSharp C#.
The strategy trades breakouts of Donchian channels, sizes positions by ATR based risk and can pyramid winning trades.

## How It Works

1. **Indicator Calculation**
   - 20 period ATR for volatility.
   - Donchian channels for `ShortPeriod` and `ExitPeriod`.
   - Optional Parabolic SAR for trailing stops.
2. **Position Sizing**
   - Each entry risks `RiskFraction` of current equity.
   - Unit size is limited by `MaxUnits`.
3. **Entry Rules**
   - Close above `ShortPeriod` high -> buy.
   - Close below `ShortPeriod` low -> sell.
4. **Pyramiding**
   - Adds new unit every `AddInterval` ATR move in favor until `MaxUnits`.
5. **Exit Rules**
   - Opposite `ExitPeriod` breakout.
   - ATR stop using `StopAtr` and optional take profit `TakeAtr`.
   - If `UseSar` is true, Parabolic SAR stop also applies.

## Parameters

- `ExitPeriod` = 10
- `ShortPeriod` = 20
- `LongPeriod` = 55
- `RiskFraction` = 0.01
- `MaxUnits` = 4
- `AddInterval` = 1
- `StopAtr` = 1
- `TakeAtr` = 1
- `UseSar` = false
- `SarStep` = 0.02
- `SarMax` = 0.2
- `CandleType` = 1 day

## Tags

- **Category**: Trend Following
- **Direction**: Both
- **Indicators**: ATR, Highest, Lowest, Parabolic SAR
- **Stops**: ATR / SAR
- **Complexity**: Intermediate
- **Timeframe**: Daily
- **Seasonality**: No
- **Neural networks**: No
- **Divergence**: No
- **Risk level**: Medium
