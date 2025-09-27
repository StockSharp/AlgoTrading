# Random Coin Toss Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This experimental strategy flips a coin every N bars and enters long or short based on the result. Risk is managed through ATR-based stop-loss and take-profit levels.

Testing indicates an average annual return of about 8%. It performs best in the crypto market.

The idea is to provide a baseline for random entries while maintaining disciplined exits.

## Details

- **Entry Criteria**: Every `EntryFrequency` bars a coin is tossed; heads go long, tails go short.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop-loss or take-profit hit.
- **Stops**: Yes.
- **Default Values**:
  - `AtrLength` = 14
  - `SlMultiplier` = 1m
  - `TpMultiplier` = 2m
  - `EntryFrequency` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Experimental
  - Direction: Both
  - Indicators: ATR
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High

