# Стратегия Ma Williams R
[English](README.md) | [中文](README_cn.md)

Реализация стратегии №164 — MA + Williams %R. Покупка, когда цена выше скользящей средней и Williams %R ниже -80 (перепроданность). Продажа, когда цена ниже скользящей средней и Williams %R выше -20 (перекупленность).

Скользящая средняя отражает направление текущего тренда. Williams %R ищет перепроданность или перекупленность относительно этого тренда.

Подходит свинг-трейдерам, ожидающим откатов к средней. Расстояние стоп-лосса определяется по ATR.

## Подробности

- **Условия входа**:
  - Длинная: `Close > MA && WilliamsR < WilliamsROversold`
  - Короткая: `Close < MA && WilliamsR > WilliamsROverbought`
- **Long/Short**: Оба
- **Условия выхода**:
  - Williams %R возвращается к середине
- **Стопы**: процентный уровень через `StopLoss`
- **Параметры по умолчанию**:
  - `MaPeriod` = 20
  - `MaType` = MovingAverageTypeEnum.Simple
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Mean reversion
  - Направление: Оба
  - Индикаторы: Moving Average, Williams %R, R
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
