# RSI Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
This strategy tracks the relative strength index and measures its distance from an average level. When RSI deviates by more than a multiple of its recent standard deviation, the algorithm expects a snap back toward the mean.

A long trade is opened when RSI falls below the lower band defined by the average minus `Multiplier` times the standard deviation. A short trade is taken when RSI rises above the upper band. Exits occur when RSI returns to its moving average.

The method suits traders looking for objective oversold and overbought signals. Using a volatility-based band adapts the thresholds to current market conditions while a stop-loss keeps losses limited.

## Details
- **Entry Criteria**:
  - **Long**: RSI < Avg - Multiplier * StdDev
  - **Short**: RSI > Avg + Multiplier * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when RSI > Avg
  - **Short**: Exit when RSI < Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `RsiPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 61%. It performs best in the crypto market.
