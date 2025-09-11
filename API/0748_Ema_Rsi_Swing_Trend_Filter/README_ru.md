# EMA RSI Swing Trend Filter
[English](README.md) | [中文](README_cn.md)

Стратегия торгует пересечение EMA20 и EMA50 по направлению тренда EMA200.
Опциональный фильтр RSI ограничивает входы, когда индикатор перекуплен или перепродан.

## Детали

- **Вход**: Пересечение EMA20 и EMA50 с учётом положения цены относительно EMA200 и опционального фильтра RSI.
- **Длинные/Короткие**: Оба направления.
- **Выход**: Опциональный выход при обратном пересечении EMA.
- **Стопы**: Нет.
- **Значения по умолчанию**:
  - `EmaFastPeriod` = 20
  - `EmaSlowPeriod` = 50
  - `EmaTrendPeriod` = 200
  - `RsiLength` = 14
  - `UseRsiFilter` = true
  - `RsiMaxLong` = 70
  - `RsiMinShort` = 30
  - `RequireCloseConfirm` = true
  - `ExitOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Trend
  - Направление: Both
  - Индикаторы: EMA, RSI
  - Стопы: Нет
  - Сложность: Basic
  - Таймфрейм: Intraday
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенции: Нет
  - Уровень риска: Medium
