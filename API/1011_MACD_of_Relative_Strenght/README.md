# MACD of Relative Strenght Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy calculates relative strength by dividing the close price by the highest high over a specified period and applies the MACD indicator to that ratio. A long position is opened when the MACD histogram is positive and closed when it turns negative. A percentage stop-loss protects the trade.

## Details
- **Entry**: Histogram > 0.
- **Exit**: Histogram < 0 or stop-loss.
- **Type**: Long only.
- **Indicators**: Highest, MACD.
