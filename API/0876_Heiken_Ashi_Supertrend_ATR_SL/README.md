# Heiken Ashi Supertrend ATR-SL Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining Heikin Ashi candles with a Supertrend direction filter. Entries require candles without wicks and can enable ATR based stop loss and break even.

## Details

- **Entry Criteria**:
  - Long: green HA candle without lower wick, optional uptrend filter
  - Short: red HA candle without upper wick, optional downtrend filter
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: red HA candle without upper wick or stop hit
  - Short: green HA candle without lower wick or stop hit
- **Stops**: ATR based with optional break even
- **Default Values**:
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `AtrFactor` = 3m
  - `UseBreakEven` = false
  - `BreakEvenAtrMultiplier` = 1m
  - `UseHardStop` = false
  - `StopLossAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: Heikin Ashi, Supertrend, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
