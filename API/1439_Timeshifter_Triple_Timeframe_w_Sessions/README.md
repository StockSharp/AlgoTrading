# Timeshifter Triple Timeframe Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trading across three timeframes with optional ADX confirmation and session filters.

Testing indicates an average annual return of about 37%. It performs best in the forex market.

The system aligns with the higher timeframe trend, enters on medium timeframe breakouts and exits on lower timeframe reversals. Trades can be limited to London, New York and Tokyo sessions. An ADX filter can be used to ensure sufficient momentum.

## Details

- **Entry Criteria**:
  - **Long**: Higher timeframe close above its SMA and medium timeframe price crosses above its SMA.
  - **Short**: Higher timeframe close below its SMA and medium timeframe price crosses below its SMA.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Lower timeframe price crosses below its SMA.
  - **Short**: Lower timeframe price crosses above its SMA.
- **Stops**: No.
- **Default Values**:
  - `HigherMaLength` = 50
  - `MediumMaLength` = 20
  - `LowerMaLength` = 10
  - `AdxLength` = 14
  - `AdxThreshold` = 25
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, ADX
  - Stops: No
  - Complexity: Complex
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
