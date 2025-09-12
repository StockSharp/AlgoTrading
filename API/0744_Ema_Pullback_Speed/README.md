# EMA Pullback Speed Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The EMA Pullback Speed strategy uses a dynamic EMA that adapts to price acceleration. A long position opens when price returns to the dynamic EMA during an uptrend with a bullish reversal and sufficient upward speed. A short position opens on the opposite conditions. Exits use ATR-based stop loss and a fixed percentage take profit.

## Details

- **Entry Criteria**:
  - **Long**: Price above dynamic EMA, bullish reversal, price returned to EMA, positive speed, short EMA above long EMA, speed ≥ `LongSpeedMin`.
  - **Short**: Price below dynamic EMA, bearish reversal, price returned to EMA, negative speed, short EMA below long EMA, speed ≤ `ShortSpeedMax`.
- **Long/Short**: Both sides.
- **Exit Criteria**: ATR stop loss and fixed percent take profit.
- **Stops**: `AtrMultiplier`×ATR stop loss, `FixedTpPct`% take profit.
- **Default Values**:
  - `MaxLength` = 50
  - `AccelMultiplier` = 3
  - `ReturnThreshold` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 4
  - `FixedTpPct` = 1.5
  - `ShortEmaLength` = 21
  - `LongEmaLength` = 50
  - `LongSpeedMin` = 1000
  - `ShortSpeedMax` = -1000
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, ATR
  - Stops: ATR stop loss, fixed take profit
  - Complexity: Medium
  - Timeframe: 5m
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
