# Strategic Multi Step Supertrend
[English](README.md) | [中文](README_cn.md)

Стратегия использует два индикатора Supertrend и многошаговый тейк-профит.

## Детали

- **Критерии входа**: Сигналы по направлению двух Supertrend.
- **Лонг/Шорт**: Настраивается.
- **Критерии выхода**: Обратный сигнал Supertrend или уровни тейк-профита.
- **Стопы**: Тейк-профит по шагам.
- **Значения по умолчанию**:
  - `UseTakeProfit` = true
  - `TakeProfitPercent1` = 6.0
  - `TakeProfitPercent2` = 12.0
  - `TakeProfitPercent3` = 18.0
  - `TakeProfitPercent4` = 50.0
  - `TakeProfitAmount1` = 12
  - `TakeProfitAmount2` = 8
  - `TakeProfitAmount3` = 4
  - `TakeProfitAmount4` = 0
  - `NumberOfSteps` = 3
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 5
  - `Factor2` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Фильтры**:
  - Категория: Trend
  - Направление: Настраивается
  - Индикаторы: ATR, Supertrend
  - Стопы: Тейк-профит
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенции: Нет
  - Уровень риска: Средний
