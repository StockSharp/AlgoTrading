# GC Strategy with Trend Filter and Sudden Move Profit Taking
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses a 5/25 SMA crossover with a 75-period trend filter and an ADX confirmation. Positions are closed when the price moves more than a specified percentage from the previous close, capturing sudden moves.

## Details
- **Entry**: Long when 5 SMA crosses above 25 SMA, price above 75 SMA, and ADX > threshold. Short when opposite.
- **Exit**: Opposite signal or sudden move exceeding configured percentage.
- **Indicators**: SMA, Average Directional Index.
- **Markets**: Any.
