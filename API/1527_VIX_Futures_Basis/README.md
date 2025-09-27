# VIX Futures Basis Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy evaluates contango and backwardation between the VIX index and the first two VIX futures contracts. When both futures trade above the index by a specified basis threshold, the strategy enters a long position on the front contract. If both futures fall below the index by the threshold, the long position is closed or a short position is opened.

## Details
- **Entry**: Contango detected (both futures above VIX by threshold).
- **Exit/Short**: Backwardation detected.
- **Data**: VIX index, front and second VIX futures.
