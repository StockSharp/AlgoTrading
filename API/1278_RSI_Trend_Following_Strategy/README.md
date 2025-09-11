# RSI Trend Following Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The RSI Trend Following strategy goes long when momentum is confirmed by RSI, Stochastic, MACD and price staying above a long-term EMA. A trailing stop activates after a favorable ATR move and follows a shorter EMA.

Positions exit when price falls below the trailing EMA or hits the ATR-based stop-loss.

## Details

- **Entry Criteria**: `K < 80 && D < 80 && MACD > Signal && RSI > 50 && Low > EMA(200)`
- **Long/Short**: Long only
- **Exit Criteria**: Price below trailing EMA or stop-loss
- **Stops**: Yes, ATR based
- **Default Values**:
  - `StopLossAtr` = 1.75
  - `TrailingActivationAtr` = 2.25
  - `RsiPeriod` = 14
  - `TrailingEmaLength` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
