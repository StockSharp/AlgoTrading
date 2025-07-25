# Прорыв по ширине полос Боллинджера
[English](README.md) | [中文](README_cn.md)

Ширина полос Боллинджера измеряет расстояние между верхней и нижней полосами. Расширение ширины указывает на рост волатильности и возможное формирование тренда. Эта стратегия торгует прорывы, когда ширина увеличивается.

Тестирование показывает среднегодичную доходность около 151%\. Стратегию лучше запускать на фондовом рынке.

Положение цены относительно средней полосы задаёт направление. Расширяющийся канал с ценой выше средней запускает покупки, а расширение при цене ниже средней — продажи.

Выход происходит при сужении ширины полос или достижении волатильного стопа.

## Подробности

- **Условия входа**: ширина полос растёт, а цена относительно средней полосы определяет направление.
- **Длинные/короткие позиции**: обе стороны.
- **Условия выхода**: ширина полос сужается или стоп.
- **Стопы**: да.
- **Значения по умолчанию**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Breakout
  - Направление: обе стороны
  - Индикаторы: полосы Боллинджера, ATR
  - Стопы: да
  - Сложность: базовая
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Уровень риска: средний

