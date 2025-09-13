# Charles Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy based on daily high and low levels. It looks for price moving beyond the previous day's range with an RSI and EMA trend filter. The strategy calculates the daily high and low, offsets them by a configurable delta, and enters long above the upper level or short below the lower level when trend conditions are confirmed.

## Details

- **Entry Criteria**:
  - Long: `Close > DailyHigh + Delta` and `RSI > 55` and `FastEMA > SlowEMA`
  - Short: `Close < DailyLow - Delta` and `RSI < 45` and `FastEMA < SlowEMA`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal or protection
- **Stops**: Configurable take profit and stop loss in percent
- **Default Values**:
  - `Delta` = 0.0002m
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `RsiPeriod` = 14
  - `TakeProfit` = 1m
  - `StopLoss` = 0.5m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: EMA, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
