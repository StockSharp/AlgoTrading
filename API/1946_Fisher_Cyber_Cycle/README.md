# Fisher Cyber Cycle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy applies the Fisher Transform to Ehlers' Cyber Cycle indicator. A long position is opened when the Fisher line crosses above its trigger line, while a short position is opened on a downward cross. Positions are closed or reversed on the opposite cross.

## Details

- **Entry Criteria**:
  - **Long**: `Fisher > Trigger` && `previous Fisher <= previous Trigger`
  - **Short**: `Fisher < Trigger` && `previous Fisher >= previous Trigger`
- **Exit Criteria**:
  - Opposite crossover of Fisher and Trigger
- **Stops**: None
- **Default Values**:
  - `Alpha` = 0.07
  - `Length` = 8
  - `Candle Type` = 8-hour timeframe
- **Filters**:
  - Category: Trend following
  - Direction: Long and Short
  - Indicators: Fisher Transform, Cyber Cycle
  - Stops: None
  - Complexity: Medium
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
