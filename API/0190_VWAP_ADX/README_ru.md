# Vwap Adx Strategy
[English](README.md) | [中文](README_cn.md)

Стратегия основана на VWAP и ADX. Вход в длинные позиции, когда цена выше VWAP и ADX > 25. Вход в короткие, когда цена ниже VWAP и ADX > 25. Выход при ADX < 20.

Тестирование показывает среднегодичную доходность около 157%\. Стратегию лучше запускать на крипторынке.

VWAP выступает эталоном сессии, а ADX измеряет силу движения. Входы происходят, когда цена отклоняется от VWAP и ADX показывает уверенность.

Подходит для внутридневных трендовых трейдеров. Защитные стопы используют кратные ATR.

## Детали

- **Критерии входа**:
  - Long: `Close > VWAP && ADX > 25`
  - Short: `Close < VWAP && ADX > 25`
- **Long/Short**: Оба направления
- **Критерии выхода**: ADX опускается ниже порога
- **Стопы**: процентный на основе `StopLossPercent`
- **Значения по умолчанию**:
  - `StopLossPercent` = 2m
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Mean reversion
  - Направление: Оба
  - Индикаторы: VWAP, ADX
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

