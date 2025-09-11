# Eliora Gold 1m Heikin Ashi Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy uses Heikin Ashi candles on a one-minute timeframe. It enters on strong trend-aligned candles when the market is not consolidating and enforces a cooldown between trades. Exits are handled by an ATR-based trailing stop.

## Details

- **Entry Criteria**: strong Heikin Ashi candle in trend direction, no consolidation, volatility filter.
- **Long/Short**: Both.
- **Exit Criteria**: ATR trailing stop.
- **Stops**: Yes.
- **Default Values**:
  - `AtrPeriod` = 14
  - `CooldownBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Heikin Ashi, ATR, SMA, Highest/Lowest
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
