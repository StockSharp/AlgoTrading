# Стратегия Manadi Buy Sell EMA MACD RSI
[English](README.md) | [中文](README_cn.md)

Стратегия пересечения EMA с подтверждением MACD и фильтром RSI. Входы по рынку и выход по фиксированным процентным стоп-лоссу и тейк-профиту.

## Подробности

- **Критерии входа**: Пересечение EMA с подтверждением MACD и RSI в пределах.
- **Длинные/Короткие**: Оба направления.
- **Критерии выхода**: Процентный стоп-лосс или тейк-профит.
- **Стопы**: Процентные.
- **Значения по умолчанию**:
  - `FastEmaLength` = 9
  - `SlowEmaLength` = 21
  - `RsiLength` = 14
  - `RsiUpperLong` = 70
  - `RsiLowerLong` = 40
  - `RsiUpperShort` = 60
  - `RsiLowerShort` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `TakeProfitPercent` = 0.03
  - `StopLossPercent` = 0.015
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Фильтры**:
  - Категория: Trend Following
  - Направление: Оба
  - Индикаторы: EMA, MACD, RSI
  - Стопы: Да
  - Сложность: Низкая
  - Таймфрейм: Внутридневной (1м)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
