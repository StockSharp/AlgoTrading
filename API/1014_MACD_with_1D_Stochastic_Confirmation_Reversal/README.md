# MACD with 1D Stochastic Confirmation Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that buys on MACD line crossing above signal with confirmation from daily Stochastic oscillator. The trade is closed when price hits an ATR-based stop loss or falls below a trailing EMA take profit.

## Details

- **Entry Criteria**:
  - Long: `MACD crosses above Signal && DailyK > DailyD && DailyK < 80`
- **Long/Short**: Long only
- **Stops**: ATR stop loss and trailing EMA take profit
- **Default Values**:
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TrailingEmaLength` = 20
  - `StopLossAtrMultiplier` = 3.25m
  - `TrailingActivationAtrMultiplier` = 4.25m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filters**:
  - Category: Reversal
  - Direction: Long
  - Indicators: MACD, Stochastic, ATR, EMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
