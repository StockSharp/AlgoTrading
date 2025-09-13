# EMA MACD Signal Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long when a fast EMA is above a slow EMA and the MACD signal line is rising. It enters short when the fast EMA is below the slow EMA and the signal line is falling. Stop-loss, take-profit and trailing stop are optional.

## Details

- **Entry Criteria**:
  - Fast EMA > Slow EMA and MACD signal increasing → Buy.
  - Fast EMA < Slow EMA and MACD signal decreasing → Sell.
- **Exit Criteria**:
  - Opposite entry signal closes position.
- **Indicators**: EMA, MACD signal.
- **Type**: Trend following.
- **Timeframe**: 5 minutes (default).
