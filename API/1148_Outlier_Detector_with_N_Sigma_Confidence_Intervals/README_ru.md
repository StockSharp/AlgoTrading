# Стратегия Outlier Detector with N-Sigma Confidence Intervals
[English](README.md) | [中文](README_cn.md)

Стратегия определяет выбросы в изменении цены с помощью N-сигмовых доверительных интервалов и торгует на возврат к среднему при экстремальных движениях.

## Детали

- **Условия входа**:
  - Продажа при z-score > `SecondLimit`.
  - Покупка при z-score < -`SecondLimit`.
- **Длинные/короткие**: Оба.
- **Условия выхода**:
  - Закрытие позиции, когда |z-score| < `FirstLimit`.
- **Стопы**: Нет.
- **Значения по умолчанию**:
  - `SampleSize` = 30
  - `FirstLimit` = 2
  - `SecondLimit` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Фильтры**:
  - Категория: Mean Reversion
  - Направление: Оба
  - Индикаторы: StandardDeviation, Z-Score
  - Стопы: Нет
  - Сложность: Базовая
  - Таймфрейм: Любой
  - Сезонность: Нет
  - Нейросети: Нет
  - Уровень риска: Средний
