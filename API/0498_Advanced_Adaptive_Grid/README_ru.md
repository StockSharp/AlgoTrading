# Стратегия Advanced Adaptive Grid
[English](README.md) | [中文](README_cn.md)

Стратегия Advanced Adaptive Grid строит динамическую сетку уровней входа, адаптируя шаг к волатильности через ATR. Направление тренда оценивается с помощью нескольких индикаторов (SMA, MACD, RSI, Momentum). Сделки открываются при достижении ценой уровней сетки в сторону тренда. Управление рисками включает фиксированный стоп‑лосс, тейк‑профит, трейлинг‑стоп, выход по времени и ограничение дневного убытка.

## Детали

- **Условия входа**:
  - В тренде: цена достигает рассчитанных уровней сетки при подтверждении RSI.
  - Во флэте: вход по RSI при перекупленности/перепроданности.
- **Лонг/Шорт**: обе стороны.
- **Условия выхода**:
  - Стоп‑лосс, тейк‑профит, трейлинг‑стоп, смена тренда или выход по времени.
- **Стопы**: фиксированный и трейлинг.
- **Значения по умолчанию**:
  - `BaseGridSize` = 1
  - `MaxPositions` = 5
  - `UseVolatilityGrid` = True
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `ShortMaLength` = 20
  - `LongMaLength` = 50
  - `SuperLongMaLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 3
  - `UseTrailingStop` = True
  - `TrailingStopPercent` = 1
  - `MaxLossPerDay` = 5
  - `TimeBasedExit` = True
  - `MaxHoldingPeriod` = 48
- **Фильтры**:
  - Категория: Сетка / Тренд
  - Направление: обе
  - Индикаторы: ATR, SMA, MACD, RSI, Momentum
  - Стопы: да
  - Сложность: высокая
  - Таймфрейм: любой
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Уровень риска: высокий
