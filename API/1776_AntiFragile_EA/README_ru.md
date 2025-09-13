# Стратегия AntiFragile EA
[English](README.md) | [中文](README_cn.md)

Грид-стратегия, размещающая лимитные ордера слоями выше и ниже текущей цены с увеличивающимся объёмом.
Позиции защищаются первоначальным стопом и сопровождаются трейлинг-стопом при движении цены.

## Детали

- **Вход**:
  - Лонг: выставление buy limit на каждом `SpaceBetweenTrades` шага ниже bid.
  - Шорт: выставление sell limit на каждом `SpaceBetweenTrades` шага выше ask.
- **Лонг/Шорт**: управляется параметрами `TradeLong` и `TradeShort`.
- **Выход**: трейлинг-стоп или исполнение противоположной стороны сетки.
- **Стопы**: начальный `StopLossPips` и трейлинг `TrailingStopPips`.
- **Значения по умолчанию**:
  - `StartingVolume` = 0.1m
  - `IncreasePercentage` = 1m
  - `SpaceBetweenTrades` = 700m
  - `NumberOfTrades` = 50
  - `StopLossPips` = 300m
  - `TrailingStopPips` = 100m
  - `TradeLong` = true
  - `TradeShort` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Фильтры**:
  - Категория: Грид
  - Направление: Оба
  - Индикаторы: Нет
  - Стопы: Трейлинг
  - Сложность: Средняя
  - Таймфрейм: Любой
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Высокий
