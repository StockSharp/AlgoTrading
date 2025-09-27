# R Based Strategy Template
[English](README.md) | [中文](README_cn.md)

Стратегия на основе RSI с управлением риском и несколькими типами стопов.

## Детали

- **Условия входа**:
  - Лонг при пересечении RSI ниже `OversoldLevel`.
  - Шорт при пересечении RSI выше `OverboughtLevel`.
- **Лонг/Шорт**: Оба направления.
- **Условия выхода**: Стоп-лосс или тейк-профит по множителю `TpRValue`.
- **Стопы**:
  - Fixed, Atr, Percentage или Ticks.
- **Значения по умолчанию**:
  - `RiskPerTradePercent` = 1
  - `RsiLength` = 14
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `StopLossType` = Fixed
  - `SlValue` = 100
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `TpRValue` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Осцилляторы
  - Направление: Оба
  - Индикаторы: RSI, ATR
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Любой
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
