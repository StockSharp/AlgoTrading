# Стратегия Supertrend Advance Pullback
[English](README.md) | [中文](README_cn.md)

Supertrend Advance Pullback сочетает Supertrend с входами по откату или смене тренда. Дополнительные фильтры EMA, RSI, MACD и CCI уточняют сигналы.

## Детали

- **Критерий входа**: откат или разворот Supertrend с фильтрами EMA, RSI, MACD, CCI
- **Длин/Шорт**: обе стороны
- **Критерий выхода**: противоположный сигнал
- **Стопы**: нет
- **Значения по умолчанию**:
  - `AtrLength` = 10
  - `Factor` = 3
  - `EmaLength` = 200
  - `UseEmaFilter` = true
  - `UseRsiFilter` = true
  - `RsiLength` = 14
  - `RsiBuyLevel` = 50
  - `RsiSellLevel` = 50
  - `UseMacdFilter` = true
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseCciFilter` = true
  - `CciLength` = 20
  - `CciBuyLevel` = 200
  - `CciSellLevel` = -200
  - `Mode` = Pullback
- **Фильтры**:
  - Категория: Тренд
  - Направление: Обе стороны
  - Индикаторы: Supertrend, EMA, RSI, MACD, CCI
  - Стопы: Нет
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
