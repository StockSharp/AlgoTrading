# Стратегия Rsi Stochastic Wma
[English](README.md) | [中文](README_cn.md)

Стратегия сочетает RSI, Stochastic Oscillator и взвешенную скользящую среднюю (WMA).
Покупает при перепроданности RSI, пересечении %K выше %D и цене выше WMA.
Продаёт при перекупленности RSI, пересечении %K ниже %D и цене ниже WMA.

## Детали

- **Критерий входа**:
  - Long: `RSI < 30 && %K пересекает выше %D && Close > WMA`
  - Short: `RSI > 70 && %K пересекает ниже %D && Close < WMA`
- **Long/Short**: Оба
- **Стопы**: Нет
- **Значения по умолчанию**:
  - `RsiLength` = 14
  - `StochK` = 14
  - `StochD` = 3
  - `WmaLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Trend following
  - Направление: Оба
  - Индикаторы: RSI, Stochastic, WMA
  - Стопы: Нет
  - Сложность: Базовая
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Риск: Средний
