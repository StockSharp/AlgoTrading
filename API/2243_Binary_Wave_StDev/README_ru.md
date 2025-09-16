# Стратегия Binary Wave StdDev
[English](README.md) | [中文](README_cn.md)

Стратегия суммирует сигналы от MA, MACD, CCI, Momentum, RSI и ADX с настраиваемыми весами.
Сделки открываются по направлению итогового балла, если волатильность (стандартное отклонение) превышает порог.
Стоп‑лосс и тейк‑профит в пунктах по желанию.

## Детали

- **Условия входа**:
  - Лонг: балл > 0 и StdDev >= EntryVolatility
  - Шорт: балл < 0 и StdDev >= EntryVolatility
- **Условия выхода**:
  - Волатильность падает ниже ExitVolatility
- **Стопы**: опционально через `UseStopLoss` и `UseTakeProfit`
- **Значения по умолчанию**:
  - `WeightMa` = 1
  - `WeightMacd` = 1
  - `WeightCci` = 1
  - `WeightMomentum` = 1
  - `WeightRsi` = 1
  - `WeightAdx` = 1
  - `MaPeriod` = 13
  - `FastMacd` = 12
  - `SlowMacd` = 26
  - `SignalMacd` = 9
  - `CciPeriod` = 14
  - `MomentumPeriod` = 14
  - `RsiPeriod` = 14
  - `AdxPeriod` = 14
  - `StdDevPeriod` = 9
  - `EntryVolatility` = 1.5
  - `ExitVolatility` = 1
  - `StopLossPoints` = 1000
  - `TakeProfitPoints` = 2000
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Фильтры**:
  - Категория: Следование тренду
  - Направление: Обе стороны
  - Индикаторы: MA, MACD, CCI, Momentum, RSI, ADX, StandardDeviation
  - Стопы: Опционально
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
