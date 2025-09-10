# Стратегия Bullish Reversal Bar
[English](README.md) | [中文](README_cn.md)

Реализация стратегии №578 — Bullish Reversal Bar. Вход в лонг происходит, когда бычья разворотная свеча формируется ниже линий Аллигатора и цена пробивает максимум этой свечи. Дополнительно можно включить фильтры по Awesome Oscillator и Market Facilitation Index.

Паттерн ищет новое минимум, закрывающееся в верхней половине свечи, и подтверждение при пробое её максимума.

## Подробности

- **Условия входа**:
  - Длинная: `bullish reversal bar && close > confirmation level`
- **Long/Short**: Только лонг
- **Условия выхода**:
  - Стоп на минимуме свечи или смена тренда вниз
- **Стопы**: минимум свечи хранится в `_stopLoss`
- **Параметры по умолчанию**:
  - `EnableAo` = false
  - `EnableMfi` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Trend following
  - Направление: Лонг
  - Индикаторы: Alligator, Awesome Oscillator, Market Facilitation Index
  - Стопы: Да
  - Сложность: Высокая
  - Таймфрейм: Краткосрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

