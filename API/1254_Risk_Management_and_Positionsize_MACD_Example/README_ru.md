# Risk Management and Positionsize - MACD example
[English](README.md) | [中文](README_cn.md)

Стратегия **Risk Management and Positionsize - MACD example** демонстрирует динамический размер позиции на основе текущей эквити. Используются пересечения MACD со старшего таймфрейма и фильтр тренда на основе скользящей средней.

## Подробности
- **Условия входа**: линия MACD пересекает сигнальную с подтверждением тренда.
- **Лонг/Шорт**: обе стороны.
- **Условия выхода**: обратное пересечение MACD.
- **Стопы**: нет.
- **Значения по умолчанию**:
  - `InitialBalance = 10000m`
  - `LeverageEquity = true`
  - `MarginFactor = -0.5m`
  - `Quantity = 3.5m`
  - `MacdMaType = MovingAverageTypeEnum.EMA`
  - `FastMaLength = 11`
  - `SlowMaLength = 26`
  - `SignalMaLength = 9`
  - `MacdTimeFrame = TimeSpan.FromMinutes(30)`
  - `TrendMaType = MovingAverageTypeEnum.EMA`
  - `TrendMaLength = 55`
  - `TrendTimeFrame = TimeSpan.FromDays(1)`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Фильтры**:
  - Категория: Следование тренду
  - Направление: Обе
  - Индикаторы: MACD, Moving Average
  - Стопы: Нет
  - Сложность: Средняя
  - Таймфрейм: Внутридневной (5m)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенции: Нет
  - Уровень риска: Средний
