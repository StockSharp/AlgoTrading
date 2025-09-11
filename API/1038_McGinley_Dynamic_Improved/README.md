# McGinley Dynamic (Improved) Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implements the "McGinley Dynamic (Improved)" indicator by John R. McGinley, Jr. and trades when the close price crosses the dynamic line. The strategy supports modern, original, and custom coefficient formulas and can optionally display the unconstrained variant for comparison.

## Details

- **Entry Long**: close crosses above McGinley Dynamic.
- **Entry Short**: close crosses below McGinley Dynamic.
- **Indicators**: McGinley Dynamic, optional Unconstrained McGinley Dynamic, EMA for reference.
- **Default Values**: Period = 14, Formula = Modern, Custom k = 0.5, Exponent = 4.
- **Direction**: Both.
