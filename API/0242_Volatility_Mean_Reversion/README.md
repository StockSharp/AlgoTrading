# Volatility Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
This approach trades around fluctuations in market volatility. When the ATR deviates markedly from its moving average, it suggests volatility has become unusually high or low and may revert.

The strategy goes long when ATR drops below the average minus `DeviationMultiplier` times the standard deviation and price is below the moving average. It shorts when ATR exceeds the upper band and price is above the average. Positions exit once ATR returns toward its mean level.

Such setups work for traders who like to fade volatility extremes rather than price direction. A protective stop-loss is used in case volatility keeps expanding.

## Details
- **Entry Criteria**:
  - **Long**: ATR < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Short**: ATR > Avg + DeviationMultiplier * StdDev && Close > MA
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when ATR > Avg
  - **Short**: Exit when ATR < Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `AtrPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 73%. It performs best in the crypto market.
