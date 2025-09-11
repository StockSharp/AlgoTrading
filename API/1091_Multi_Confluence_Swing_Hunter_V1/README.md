# Multi-Confluence Swing Hunter V1 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Multi-Confluence Swing Hunter V1 strategy uses a scoring system combining RSI, MACD, and price action to identify swing lows and highs. A long trade opens when bullish signals reach the minimum entry score and closes when bearish signals reach the exit score.

## Details

- **Entry Criteria**: Entry score ≥ `MinEntryScore` from RSI/MACD signals and bullish candle structure.
- **Long/Short**: Long only.
- **Exit Criteria**: Exit score ≥ `MinExitScore` from RSI/MACD signals and bearish candle structure.
- **Stops**: No.
- **Default Values**:
  - `MacdFast` = 3
  - `MacdSlow` = 10
  - `MacdSignal` = 3
  - `RsiLength` = 21
  - `MinEntryScore` = 13
  - `MinExitScore` = 13
  - `MinLowerWickPercent` = 50
  - `RsiOversold` = 30
  - `RsiExtremeOversold` = 25
  - `RsiOverbought` = 70
  - `RsiExtremeOverbought` = 75
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Reversal
  - Direction: Long
  - Indicators: RSI, MACD
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
