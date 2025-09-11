# External Signals Tester Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Opens positions when the difference between fast and slow EMA crosses zero or when price crosses optional EMA lines. Supports optional percent-based stop loss, take profit, and breakeven.

## Details

- **Entry Criteria**: EMA(10) - EMA(30) crossing zero or price crossing configurable EMA.
- **Long/Short**: Long and short.
