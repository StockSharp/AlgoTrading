# Интеллектуальный аккумулятор TTP
[English](README.md) | [中文](README_cn.md)

Стратегия накапливает длинные позиции, когда RSI опускается ниже своего среднего на одно стандартное отклонение, и распределяет их, когда RSI поднимается выше этого уровня.

## Детали

- **Критерии входа**: RSI < SMA(RSI, `MaPeriod`) - StdDev(RSI, `StdPeriod`)
- **Длинные/короткие**: только длинные
- **Критерии выхода**: RSI > SMA(RSI, `MaPeriod`) + StdDev(RSI, `StdPeriod`) и прибыль выше `MinProfit`
- **Стопы**: нет
- **Значения по умолчанию**:
  - `RsiPeriod` = 7
  - `MaPeriod` = 14
  - `StdPeriod` = 14
  - `AddWhileInLossOnly` = true
  - `MinProfit` = 0m
  - `ExitPercent` = 100m
  - `UseDateFilter` = false
  - `StartDate` = 2022-06-01
  - `EndDate` = 2030-07-01
  - `CandleType` = TimeSpan.FromHours(1)
- **Фильтры**:
  - Категория: Mean Reversion
  - Направление: Длинные
  - Индикаторы: RSI, MA, StdDev
  - Стопы: Нет
  - Сложность: Базовая
  - Таймфрейм: Внутридневной (1h)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
