# Quarterly Expiry Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Quarterly Expiry weeks see futures and options contracts roll over, often creating volatility as positions are closed or rolled.
Price swings can accelerate as hedges are adjusted and liquidity temporarily dries up.

Testing indicates an average annual return of about 115%. It performs best in the stocks market.

The strategy trades in the direction of the prevailing trend at the start of the week, exiting before settlement day to avoid chaos.

A fixed stop keeps risk in line if volatility proves too extreme.

## Details

- **Entry Criteria**: calendar effect triggers
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Seasonality
  - Direction: Both
  - Indicators: Seasonality
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

