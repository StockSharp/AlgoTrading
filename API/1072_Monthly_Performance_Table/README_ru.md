# Стратегия Monthly Performance Table
[English](README.md) | [中文](README_cn.md)

Торгует, когда ADX находится между +DI и -DI, а оба отклонения от ADX превышают настраиваемые пороги.

## Детали

- **Условия входа**:
  - Лонг, когда |+DI - ADX| ≥ `LongDifference` и |-DI - ADX| ≥ `LongDifference`, а ADX между +DI и -DI.
  - Шорт, когда |+DI - ADX| ≥ `ShortDifference` и |-DI - ADX| ≥ `ShortDifference`, а ADX между -DI и +DI.
- **Длинные/Короткие**: Оба.
- **Условия выхода**: Обратный сигнал.
- **Стопы**: Нет.
- **Значения по умолчанию**:
  - `Length` = 14
  - `LongDifference` = 10
  - `ShortDifference` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **Фильтры**:
  - Категория: Trend
  - Направление: Оба
  - Индикаторы: ADX, DMI
  - Стопы: Нет
  - Сложность: Базовая
  - Таймфрейм: Любой
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Риск: Средний
