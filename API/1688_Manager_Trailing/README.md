# Manager Trailing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy opens a single long position and then manages it using several risk controls:

- Percentage-based **take profit** and **stop loss**.
- Profit **trailing** that activates after a configurable gain.
- **Partial closing** at custom profit levels.

The algorithm demonstrates how to manage an existing position with StockSharp using only candle data.

## Details

- **Entry Criteria**: Buy market on the first finished candle.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Take profit percentage.
  - Stop loss percentage.
  - Trailing profit trigger.
  - Partial closing portions.
- **Stops**: Yes, via percentages.
- **Filters**: None.
