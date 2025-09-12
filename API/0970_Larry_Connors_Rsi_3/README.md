# Larry Connors RSI 3
[Русский](README_ru.md) | [中文](README_cn.md)

Mean reversion strategy based on Larry Connors' RSI rules.

The strategy buys when price is above the 200-period SMA and the 2-period RSI has dropped three days in a row from above a trigger level into oversold territory. Positions exit when RSI rises above the overbought level.

## Details

- **Entry Criteria**: Close above SMA and 2-period RSI dropping three days from above trigger into oversold.
- **Long/Short**: Long only.
- **Exit Criteria**: RSI above overbought level.
- **Stops**: No.
- **Default Values**:
  - `RsiPeriod` = 2
  - `SmaPeriod` = 200
  - `DropTrigger` = 60m
  - `OversoldLevel` = 10m
  - `OverboughtLevel` = 70m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Long
  - Indicators: RSI, SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
