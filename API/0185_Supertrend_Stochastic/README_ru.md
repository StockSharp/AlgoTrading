# Supertrend Stochastic Strategy
[English](README.md) | [中文](README_cn.md)

Стратегия Supertrend + Stochastic. Стратегия входит в сделки, когда Supertrend указывает направление тренда и Stochastic подтверждает условия перепроданности/перекупленности.

Supertrend отмечает тренд, а Stochastic показывает временные обратные движения. Вход происходит, когда Stochastic выходит из зоны перепроданности или перекупленности против тренда.

Лучше всего подходит трейдерам-моментумщикам, которым нужны чёткие сигналы тренда. Значения ATR определяют дистанцию стопа.

## Детали

- **Критерии входа**:
  - Long: `Close > Supertrend && StochK < 20`
  - Short: `Close < Supertrend && StochK > 80`
- **Long/Short**: Оба направления
- **Критерии выхода**: разворот Supertrend
- **Стопы**: использование Supertrend в качестве трейлинг-стопа
- **Значения по умолчанию**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Mean reversion
  - Направление: Оба
  - Индикаторы: Supertrend, Stochastic Oscillator
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
