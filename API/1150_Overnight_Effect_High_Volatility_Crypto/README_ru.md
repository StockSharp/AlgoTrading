# Стратегия Overnight Effect High Volatility Crypto
[English](README.md) | [中文](README_cn.md)

Стратегия открывает длинную позицию вечером в дни повышенной волатильности и закрывает её перед полуночью. Волатильность измеряется стандартным отклонением логарифмической доходности за заданный период и сравнивается с медианой исторической волатильности.

## Детали

- **Условия входа**:
  - `currentHour == EntryHour && highVolatility` при активном `UseVolatilityFilter`
  - `currentHour == EntryHour` если фильтр отключён
- **Long/Short**: Long
- **Стопы**: Нет
- **Значения по умолчанию**:
  - `VolatilityPeriodDays` = 30
  - `MedianPeriodDays` = 208
  - `EntryHour` = 21
  - `ExitHour` = 23
  - `UseVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Фильтры**:
  - Категория: Время
  - Направление: Long
  - Индикаторы: StandardDeviation, Median
  - Стопы: Нет
  - Сложность: Новичок
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Низкий
