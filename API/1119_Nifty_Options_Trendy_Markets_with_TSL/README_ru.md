# Nifty Options Trendy Markets with TSL Strategy
[English](README.md) | [中文](README_cn.md)

Стратегия пробоя на основе полос Боллинджера с фильтрами ADX и Supertrend. Входы требуют всплеска объёма. Позиции закрываются при пересечении MACD, ослаблении ADX или трейлинг-стопе на основе ATR.

## Детали

- **Критерии входа**:
  - Long: цена пересекает верхнюю полосу Боллинджера сверху && ADX > порога && всплеск объёма && цена выше Supertrend
  - Short: цена пересекает нижнюю полосу Боллинджера снизу && ADX > порога && всплеск объёма && цена ниже Supertrend
- **Long/Short**: Оба направления
- **Критерии выхода**: пересечение MACD, падение ADX или ATR трейлинг-стоп
- **Стопы**: ATR трейлинг-стоп
- **Значения по умолчанию**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2m
  - `AdxLength` = 14
  - `AdxEntryThreshold` = 25m
  - `AdxExitThreshold` = 20m
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5m
  - `VolumeSpikeMultiplier` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Фильтры**:
  - Категория: Trend
  - Направление: Оба
  - Индикаторы: Bollinger Bands, ADX, Supertrend, MACD, ATR
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
