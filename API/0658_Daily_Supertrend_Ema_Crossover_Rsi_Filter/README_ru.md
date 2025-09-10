# Стратегия Daily Supertrend Ema Crossover Rsi Filter
[English](README.md) | [中文](README_cn.md)

Стратегия торгует пересечения EMA только в направлении Supertrend и при благоприятном значении RSI. Используются уровни стоп-лосса и тейк-профита на основе ATR.

## Детали

- **Условия входа**:
  - Лонг: `Fast EMA` пересекает `Slow EMA` снизу вверх, Supertrend в ап-тренде, `RSI < RsiOverbought`
  - Шорт: `Fast EMA` пересекает `Slow EMA` сверху вниз, Supertrend в даун-тренде, `RSI > RsiOversold`
- **Лонг/Шорт**: Оба направления
- **Условия выхода**: стоп-лосс или тейк-профит по ATR
- **Стопы**: Да
- **Значения по умолчанию**:
  - `FastEmaLength` = 3
  - `SlowEmaLength` = 6
  - `AtrLength` = 3
  - `StopLossMultiplier` = 2.5m
  - `TakeProfitMultiplier` = 4m
  - `RsiLength` = 10
  - `RsiOverbought` = 65m
  - `RsiOversold` = 30m
  - `SupertrendMultiplier` = 1m
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame()
- **Фильтры**:
  - Категория: Тренд
  - Направление: Оба
  - Индикаторы: EMA, Supertrend, RSI, ATR
  - Стопы: ATR множители
  - Сложность: Средняя
  - Таймфрейм: Долгосрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
