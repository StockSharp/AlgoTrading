# X2MA JJRSX Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that combines a dual moving average trend filter with an RSI based entry trigger.
The trend is defined on a higher timeframe by comparing a fast and slow moving average.
Entries are executed on a lower timeframe when RSI exits oversold or overbought zones in the direction of the trend.

## Details

- **Entry Criteria**:
  - Long: trend up and RSI crosses above `Oversold`
  - Short: trend down and RSI crosses below `Overbought`
- **Long/Short**: Both
- **Exit Criteria**: Opposite RSI threshold or trend reversal
- **Stops**: None
- **Default Values**:
  - `TrendCandleType` = 4h candles
  - `SignalCandleType` = 30m candles
  - `FastMaPeriod` = 12
  - `SlowMaPeriod` = 5
  - `RsiPeriod` = 8
  - `Overbought` = 70
  - `Oversold` = 30

