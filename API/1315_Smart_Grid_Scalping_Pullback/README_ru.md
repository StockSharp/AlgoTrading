# Smart Grid Scalping Pullback Strategy
[English](README.md) | [中文](README_cn.md)

Скальпинговая стратегия с сеткой ценовых уровней на основе ATR, расширяемых от цены двадцать баров назад. Для входов по откату используется фильтр RSI. Позиции закрываются по целевой прибыли или трейлинг-стопу на ATR.

## Детали

- **Критерии входа**:
  - Long: close < basePrice - (LongLevel + 1) * ATR * GridFactor && диапазон/low > NoTradeZone && RSI < MaxRsiLong && close > open
  - Short: close > basePrice + (ShortLevel + 1) * ATR * GridFactor && диапазон/high > NoTradeZone && RSI > MinRsiShort && close < open
- **Long/Short**: Оба направления
- **Критерии выхода**: целевая прибыль или ATR трейлинг-стоп
- **Стопы**: ATR трейлинг-стоп
- **Значения по умолчанию**:
  - `AtrLength` = 10
  - `GridFactor` = 0.35m
  - `ProfitTarget` = 0.004m
  - `NoTradeZone` = 0.003m
  - `ShortLevel` = 5
  - `LongLevel` = 5
  - `MinRsiShort` = 70
  - `MaxRsiLong` = 30
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Фильтры**:
  - Категория: Scalping
  - Направление: Оба
  - Индикаторы: ATR, RSI
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Краткосрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
