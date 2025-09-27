# Стратегия Supertrend Ema Vol
[English](README.md) | [中文](README_cn.md)

Стратегия сочетает Supertrend с подтверждением тренда по EMA и фильтром объёма. Входит при смене направления Supertrend, когда цена находится выше или ниже EMA и объём превышает свою EMA. Использует стоп-лосс на основе ATR.

## Детали

- **Условия входа**:
  - Лонг: Supertrend разворачивается вверх, цена выше EMA, объём выше Volume EMA
  - Шорт: Supertrend разворачивается вниз, цена ниже EMA, объём выше Volume EMA
- **Лонг/Шорт**: Настраивается
- **Условия выхода**: Разворот Supertrend или стоп-лосс по ATR
- **Стопы**: Кратные ATR
- **Значения по умолчанию**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `EmaLength` = 21
  - `StartDate` = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)
  - `AllowLong` = true
  - `AllowShort` = false
  - `SlMultiplier` = 2m
  - `UseVolumeFilter` = true
  - `VolumeEmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Фильтры**:
  - Категория: Trend
  - Направление: Both
  - Индикаторы: Supertrend, EMA, Volume EMA, ATR
  - Стопы: ATR
  - Сложность: Intermediate
  - Таймфрейм: Intraday
  - Сезонность: No
  - Нейросети: No
  - Дивергенция: No
  - Уровень риска: Medium
