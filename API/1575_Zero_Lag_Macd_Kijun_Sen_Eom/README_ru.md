# Zero Lag MACD + Kijun-sen + EOM Strategy
[English](README.md) | [中文](README_cn.md)

Стратегия объединяет Zero Lag MACD, линию Киджун-сен и индикатор Ease of Movement. Использует стоп и тейк по ATR.

## Детали

- **Критерии входа**: пересечение MACD с фильтрами по Киджун-сен и EOM.
- **Длинные/короткие**: оба направления.
- **Критерии выхода**: стоп или тейк по ATR.
- **Стопы**: да.
- **Значения по умолчанию**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdEmaLength` = 9
  - `KijunPeriod` = 26
  - `EomLength` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.5m
  - `RiskReward` = 1.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Тренд
  - Направление: Оба
  - Индикаторы: MACD, Donchian, EOM, ATR
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной (5m)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
