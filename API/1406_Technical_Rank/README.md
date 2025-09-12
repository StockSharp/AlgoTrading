# Technical Rank
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy computes a composite technical rank from moving averages, rate of change, PPO slope and RSI. Long positions open when the rank exceeds an upper threshold, shorts when it falls below a lower threshold.

## Details

- **Entry Criteria**: rank > UpperThreshold → long; rank < LowerThreshold → short.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `UpperThreshold` = 70
  - `LowerThreshold` = 30
  - `CandleType` = 1-minute candles
