# Profitable SuperTrend + MA + Stoch Strategy
[English](README.md) | [中文](README_cn.md)

Стратегия, сочетающая SuperTrend, пересечение EMA и осциллятор Stochastic.

Она стремится ловить тренды, определяемые SuperTrend, подтверждая входы пересечением EMA и уровнями Stochastic. Включает опциональные уровни тейк-профита и стоп-лосса.

## Детали

- **Условия входа**: тренд по SuperTrend, пересечение EMA, пороги Stochastic.
- **Длинные/короткие**: в обе стороны.
- **Условия выхода**: обратное пересечение EMA или TP/SL.
- **Стопы**: да.
- **Значения по умолчанию**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `MaFastPeriod` = 9
  - `MaSlowPeriod` = 21
  - `StochKPeriod` = 14
  - `StochDPeriod` = 3
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Тренд
  - Направление: Оба
  - Индикаторы: SuperTrend, EMA, Stochastic
  - Стопы: Да
  - Сложность: Базовая
  - Таймфрейм: Внутридневной (5м)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
