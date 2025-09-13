# YURAZ CLOSEPRC V3
[Русский](README_ru.md) | [中文](README_cn.md)

This risk management helper closes the current position once the portfolio profit grows beyond a configurable percentage of the initial capital. It emulates the original MetaTrader script `YURAZ_CLOSEPRC_V3_1.mq5`, which offered a button to exit all trades after reaching a profit goal. The strategy checks the portfolio value on every finished candle and sends a market order to close the position when the target is achieved.

## Details

- **Purpose**: close position when profit target is reached
- **Trading**: demonstration
- **Indicators**: none
- **Stops**: no
- **Default Values**:
  - `ProfitPercent` = 10
  - `CandleType` = 1 minute

## Notes

- Profit is calculated as the percentage change of `Portfolio.CurrentValue` relative to the starting value.
- After the threshold is reached the strategy issues a market order to close the entire position.
