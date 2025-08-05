# ADX MACD Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
ADX MACD blends trend strength from the Average Directional Index with momentum shifts from MACD.
When ADX is rising, breakouts have a higher chance of continuing, especially if MACD crosses in the same direction.

Testing indicates an average annual return of about 139%. It performs best in the stocks market.

The strategy trades those aligned signals and exits once ADX starts to weaken or MACD flips against the position.

A modest percent stop contains losses during choppy markets.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: ADX, MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

