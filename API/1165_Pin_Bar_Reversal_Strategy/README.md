# Pin Bar Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses pin bar candles with a trend filter and ATR-based stops and targets. A bullish pin bar above the SMA opens a long position, while a bearish one below it opens a short. Entries are skipped when volatility is too low.

## Details

- **Entry Criteria**: Pin bar in trend direction with long wick, small body and ATR above `MinAtr`.
- **Long/Short**: Both.
- **Exit Criteria**: ATR-based stop-loss or take-profit.
- **Stops**: Yes, ATR multiples.
- **Default Values**:
  - `TrendLength` = 50
  - `MaxBodyPct` = 0.30
  - `MinWickPct` = 0.66
  - `AtrLength` = 14
  - `StopMultiplier` = 1
  - `TakeMultiplier` = 1.5
  - `MinAtr` = 0.0015
  - `CandleType` = 1 hour
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: SMA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
