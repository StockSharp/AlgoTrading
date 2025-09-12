# Supertrend And MACD
[English](README.md) | [中文](README_cn.md)

Стратегия, объединяющая Supertrend, MACD и фильтр EMA 200.

## Детали

- **Условия входа**: Цена относительно Supertrend и EMA, линия MACD против сигнальной.
- **Лонг/Шорт**: Оба направления.
- **Условия выхода**: Пересечение MACD или стоп по недавним экстремумам.
- **Стопы**: Скользящие уровни Highest/Lowest.
- **Значения по умолчанию**:
  - `AtrPeriod` = 10
  - `Factor` = 3
  - `EmaPeriod` = 200
  - `StopLookback` = 10
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Фильтры**:
  - Категория: Trend
  - Направление: Both
  - Индикаторы: SuperTrend, EMA, MACD, Highest, Lowest
  - Стопы: Yes
  - Сложность: Basic
  - Таймфрейм: Intraday (1m)
  - Сезонность: No
  - Нейросети: No
  - Дивергенция: No
  - Уровень риска: Medium
