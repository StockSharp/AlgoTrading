# VWAP RSI Scalper FINAL v1
[English](README.md) | [中文](README_cn.md)

Скальпинговая стратегия, сочетающая VWAP и RSI, с выходами по ATR и дневным лимитом сделок.

## Детали

- **Условия входа**: Цена относительно VWAP и EMA с порогами RSI внутри сессии.
- **Длинные/короткие**: Оба направления.
- **Условия выхода**: Стоп и цель на основе ATR.
- **Стопы**: Да.
- **Значения по умолчанию**:
  - `RsiLength` = 3
  - `RsiOversold` = 35m
  - `RsiOverbought` = 70m
  - `EmaLength` = 50
  - `SessionStart` = 09:00
  - `SessionEnd` = 16:00
  - `MaxTradesPerDay` = 3
  - `AtrLength` = 14
  - `StopAtrMult` = 1m
  - `TargetAtrMult` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Фильтры**:
  - Категория: Скальпинг
  - Направление: Оба
  - Индикаторы: VWAP, RSI, EMA, ATR
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной (1м)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Риск: Средний
