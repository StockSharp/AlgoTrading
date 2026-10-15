# Стратегия Optimized Grid with KNN
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия открывает длинные позиции, когда быстрая линия T3 пересекает медленную сверху и среднее изменение цены по KNN положительное. Порог входа и выхода корректируется в зависимости от среднего изменения. Позиции закрываются, когда быстрая линия T3 пересекает медленную снизу и цена превышает целевой уровень прибыли.

- **Условия входа**: `t3Fast > t3Slow` и `averageChange > 0`
- **Условия выхода**: `t3Fast < t3Slow` и `(close - lastEntryPrice)/lastEntryPrice > adjustedCloseTh`
- **Индикаторы**: T3
