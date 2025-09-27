# Supertrend Ema Vol Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining Supertrend with EMA trend confirmation and volume filter. Enters on Supertrend reversals when price is above or below the EMA and volume exceeds its EMA. Implements ATR-based stop loss.

## Details

- **Entry Criteria**:
  - Long: Supertrend turns up, price above EMA, volume above Volume EMA
  - Short: Supertrend turns down, price below EMA, volume above Volume EMA
- **Long/Short**: Configurable
- **Exit Criteria**: Supertrend reversal or ATR-based stop loss
- **Stops**: ATR multiple
- **Default Values**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `EmaLength` = 21
  - `StartDate` = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)
  - `AllowLong` = true
  - `AllowShort` = false
  - `SlMultiplier` = 2m
  - `UseVolumeFilter` = true
  - `VolumeEmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Supertrend, EMA, Volume EMA, ATR
  - Stops: ATR
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
