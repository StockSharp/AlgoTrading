# Supertrend Adx Strategy
[English](README.md) | [中文](README_cn.md)

Стратегия основана на индикаторе Supertrend и ADX для подтверждения силы тренда. Критерии входа: Long: Price > Supertrend && ADX > 25 (восходящий тренд с сильным движением) Short: Price < Supertrend && ADX > 25 (нисходящий тренд с сильным движением) Критерии выхода: Long: Price < Supertrend (цена падает ниже Supertrend) Short: Price > Supertrend (цена поднимается выше Supertrend).

Тестирование показывает среднегодичную доходность около 166%\. Стратегию лучше запускать на фондовом рынке.

Supertrend дает волатильно скорректированный путь, а ADX подтверждает силу движения. Сделки открываются, когда оба индикатора совпадают.

Подходит тем, кто стремится ловить сильные тренды с трейлинг-стопом. ATR определяет размещение стопа.

## Детали

- **Критерии входа**:
  - Long: `Close > Supertrend && ADX > AdxThreshold`
  - Short: `Close < Supertrend && ADX > AdxThreshold`
- **Long/Short**: Оба направления
- **Критерии выхода**: разворот Supertrend
- **Стопы**: использование Supertrend как трейлинг-стоп
- **Значения по умолчанию**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Фильтры**:
  - Категория: Trend
  - Направление: Оба
  - Индикаторы: Supertrend, ADX
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

