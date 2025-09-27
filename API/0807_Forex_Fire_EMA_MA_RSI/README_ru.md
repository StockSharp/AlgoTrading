# Стратегия Forex Fire EMA MA RSI
[English](README.md) | [中文](README_cn.md)

Многофреймовая трендовая стратегия, использующая EMA, MA и RSI. Свечи 4h применяются для подтверждения, вход осуществляется на 15m.

## Детали

- **Условия входа**:
  - Лонг: короткая EMA выше длинной, цена выше MA, быстрый RSI выше медленного и >50, рост объёма, подтверждение старшего фрейма.
  - Шорт: противоположные условия.
- **Лонг/Шорт**: Оба направления.
- **Условия выхода**:
  - Пересечение EMA или достижение уровней RSI.
  - Опционально стоп‑лосс, тейк‑профит, трейлинг и выход по ATR.
- **Стопы**: Да, настраиваемые.
- **Значения по умолчанию**:
  - `EmaShortLength` = 13
  - `EmaLongLength` = 62
  - `MaLength` = 200
  - `MaType` = MovingAverageTypeEnum.Simple
  - `RsiSlowLength` = 28
  - `RsiFastLength` = 7
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
  - `UseTrailingStop` = true
  - `TrailingPercent` = 1.5
  - `UseAtrExits` = true
  - `AtrMultiplier` = 2
  - `AtrLength` = 14
  - `EntryCandleType` = TimeSpan.FromMinutes(15).TimeFrame()
  - `ConfluenceCandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Фильтры**:
  - Категория: Тренд
  - Направление: Оба
  - Индикаторы: EMA, MA, RSI, ATR
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Многофреймовый
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
