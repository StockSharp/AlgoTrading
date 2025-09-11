# Long EMA Advanced Exit
[English](README.md) | [中文](README_cn.md)

**Long EMA Advanced Exit** — лонговая стратегия, входящая при пересечении короткой скользящей средней выше средней и цене выше длинной. Выход осуществляется при пересечении MACD вниз, закрытии ниже выбранной скользящей, пересечении короткой средней ниже средней, трейлинг-стопе или фильтре волатильности на основе ATR.

## Подробности
- **Данные**: ценовые свечи.
- **Условия входа**:
  - **Лонг**: короткая MA пересекает среднюю снизу вверх и цена выше длинной MA.
- **Условия выхода**: пересечение MACD вниз, закрытие ниже выбранной MA, пересечение короткой MA ниже средней MA, опциональный трейлинг-стоп.
- **Стопы**: опциональный трейлинг-стоп.
- **Параметры по умолчанию**:
  - `MaType` = EMA
  - `EntryConditionType` = Crossover
  - `LongTermPeriod` = 200
  - `ShortTermPeriod` = 5
  - `MidTermPeriod` = 10
  - `EnableMacdExit` = true
  - `MacdCandleType` = TimeSpan.FromDays(7).TimeFrame()
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 15
  - `UseMaCloseExit` = false
  - `MaCloseExitPeriod` = 50
  - `UseMaCrossExit` = true
  - `UseVolatilityFilter` = false
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: трендовая
  - Направление: только лонг
  - Индикаторы: MA, MACD, ATR
  - Сложность: средняя
  - Уровень риска: средний
