# Fisher Transform X2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Fisher Transform indicator on two different timeframes. The higher timeframe defines the overall trend, while the lower timeframe generates entries when Fisher crosses its previous value against that trend. Optional parameters allow closing positions on trend change or on cross signals.

## Details

- **Entry Criteria**:
  - **Long**: `Trend Fisher rising` && `Signal Fisher crosses below its previous value`
  - **Short**: `Trend Fisher falling` && `Signal Fisher crosses above its previous value`
- **Long/Short**: Both
- **Exit Criteria**:
  - Optional close on trend reversal
  - Optional close on opposite Fisher cross on signal timeframe
- **Stops**: Take profit and stop loss in points
- **Default Values**:
  - `Trend Length` = 10
  - `Signal Length` = 10
  - `Trend Timeframe` = 6 hours
  - `Signal Timeframe` = 30 minutes
  - `Take Profit` = 2000 points
  - `Stop Loss` = 1000 points
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Fisher Transform
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
