# ICT NY Kill Zone Auto Trading
[English](README.md) | [中文](README_cn.md)

Стратегия, торгующая в нью-йоркской kill zone, используя fair value gap и order block.

## Детали

- **Условия входа**: Fair value gap и order block внутри kill zone.
- **Лонг/шорт**: Оба направления.
- **Условия выхода**: Защита позиции.
- **Стопы**: Да.
- **Значения по умолчанию**:
  - `StopLoss` = 30
  - `TakeProfit` = 60
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Breakout
  - Направление: Оба
  - Индикаторы: Price Action
  - Стопы: Да
  - Сложность: Базовая
  - Таймфрейм: Внутридневной (5m)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

