# Mean Reversion V-F Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy buys in up to five layers when price falls below a moving average by predefined percentages. Positions are closed at a fixed take profit with optional trailing.

## Details

- **Entry**: Price crosses below deviation levels from the selected moving average.
- **Exit**: Target profit is reached or trailing stop is hit.
- **Indicators**: Moving Average.
- **Direction**: Long only.
- **Stops**: Take profit, optional trailing.
