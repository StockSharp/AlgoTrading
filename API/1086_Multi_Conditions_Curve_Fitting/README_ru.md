# Стратегия Multi Conditions Curve Fitting
[English](README.md) | [中文](README_cn.md)

Комбинирует пересечение EMA, RSI и стохастический осциллятор для входа при совпадении нескольких сигналов.

## Детали

- **Условия входа**:
  - Long: `FastEMA > SlowEMA` и `RSI < RsiOversold` и `StochK < 20`
  - Short: `FastEMA < SlowEMA` и `RSI > RsiOverbought` и `StochK > 80`
- **Длинная/короткая**: обе
- **Условия выхода**:
  - Long: `FastEMA < SlowEMA` или `RSI > RsiOverbought` или `StochK > StochD`
  - Short: `FastEMA > SlowEMA` или `RSI < RsiOversold` или `StochK < StochD`
- **Стопы**: нет
- **Значения по умолчанию**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 25
  - `RsiLength` = 14
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `StochLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Фильтры**:
  - Категория: Следование тренду
  - Направление: обе
  - Индикаторы: EMA, RSI, Stochastic
  - Стопы: нет
  - Сложность: базовая
  - Таймфрейм: краткосрочный
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Риск: средний
