# Three Black Crows Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Three Black Crows is the bearish counterpart to Three White Soldiers, consisting of three long down candles after an advance.
The pattern suggests that sellers have seized control as each close lands near the session low.

This strategy initiates a short position once the third crow appears, expecting momentum to continue lower.
It can also be used to exit longs that were opened by other systems if the pattern forms at resistance.

Risk is managed with a tight percent stop above the pattern high, and trades exit if price closes back above that level.

## Details

- **Entry Criteria**: pattern match
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 178%. It performs best in the stocks market.
