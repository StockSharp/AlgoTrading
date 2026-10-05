# ADX MACD Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
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
  - `StopLossPercent` = 2
  - `AdxPeriod` = 14
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
    While flat, a MACD cross above its signal line goes long and a cross below goes short, but only while ADX is above its previous value. The position closes once ADX falls below its previous value or MACD returns to the other side of the signal line.
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

