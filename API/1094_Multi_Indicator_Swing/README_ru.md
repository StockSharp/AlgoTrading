# Multi Indicator Swing
[English](README.md) | [中文](README_cn.md)

Свинг-стратегия, объединяющая Parabolic SAR, SuperTrend, ADX и подтверждение по объёмной дельте.

## Детали

- **Условия входа**: Все включённые индикаторы дают сигнал в одном направлении.
- **Лонг/Шорт**: Оба направления.
- **Условия выхода**: Противоположный сигнал или достижение стоп-лосса/тейк-профита.
- **Стопы**: Процентные уровни по желанию.
- **Параметры по умолчанию**:
  - `CandleType` = TimeSpan.FromMinutes(2)
  - `PsarStart` = 0.02m
  - `PsarIncrement` = 0.02m
  - `PsarMaximum` = 0.2m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `DeltaLength` = 14
  - `DeltaSmooth` = 3
  - `DeltaThreshold` = 0.5m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **Фильтры**:
  - Категория: Trend
  - Направление: Both
  - Индикаторы: PSAR, SuperTrend, ADX, Volume
  - Стопы: Yes
  - Сложность: Intermediate
  - Таймфрейм: Intraday (2m)
  - Сезонность: No
  - Нейросети: No
  - Дивергенция: No
  - Уровень риска: Medium
