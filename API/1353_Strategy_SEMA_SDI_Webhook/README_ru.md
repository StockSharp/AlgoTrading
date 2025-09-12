# Strategy Sema Sdi Webhook Strategy
[English](README.md) | [中文](README_cn.md)

Стратегия основана на сглаженном пересечении EMA и подтверждении через сглаженный индикатор направленного движения.
Покупает при условиях +DI > -DI и быстрая EMA > медленной. Продаёт при -DI > +DI и быстрая EMA < медленной.

## Детали

- **Критерии входа**:
  - Long: `+DI > -DI && FastEMA > SlowEMA`
  - Short: `+DI < -DI && FastEMA < SlowEMA`
- **Long/Short**: оба направления
- **Критерии выхода**: тейк-профит, стоп-лосс, трейлинг
- **Стопы**: TP, SL, трейлинг
- **Значения по умолчанию**:
  - `FastEmaLength` = 58
  - `SlowEmaLength` = 70
  - `SmoothLength` = 3
  - `DiLength` = 1
  - `TakeProfitPercent` = 25
  - `StopLossPercent` = 4.8
  - `TrailingPercent` = 1.9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Trend
  - Направление: Оба
  - Индикаторы: EMA, Directional Index
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Краткосрочный
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
