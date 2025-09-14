# Стратегия Color J Variation
[English](README.md) | [中文](README_cn.md)

Стратегия повторяет советник ColorJVariation на основе Jurik Moving Average. Отслеживает направление JMA и входит при смене тренда. Поддерживает абсолютные стоп‑лосс и тейк‑профит.

## Подробности

- **Условия входа**:
  - Длинная: `PrevSlopeDown && JMA turns up`
  - Короткая: `PrevSlopeUp && JMA turns down`
- **Long/Short**: Оба
- **Условия выхода**:
  - Противоположный сигнал разворота
- **Стопы**: абсолютные через `StopLoss` и `TakeProfit`
- **Параметры по умолчанию**:
  - `JmaPeriod` = 12
  - `JmaPhase` = 100
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Фильтры**:
  - Категория: Trend reversal
  - Направление: Оба
  - Индикаторы: Jurik Moving Average
  - Стопы: Да
  - Сложность: Базовая
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
