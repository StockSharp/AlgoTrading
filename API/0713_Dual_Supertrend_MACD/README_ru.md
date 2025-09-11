# Dual Supertrend MACD
[English](README.md) | [中文](README_cn.md)

Стратегия **Dual Supertrend MACD** сочетает два индикатора Supertrend и фильтр MACD.
Длинная позиция открывается, когда цена выше обеих линий Supertrend и гистограмма MACD положительная.
Короткая позиция появляется при цене ниже обеих линий и отрицательной гистограмме.
Выход происходит, когда любая линия Supertrend меняет направление или гистограмма MACD пересекает ноль.

## Подробности
- **Данные**: свечи цены.
- **Условия входа**:
  - Лонг: `Close > Supertrend1 && Close > Supertrend2 && MACD Histogram > 0`
  - Шорт: `Close < Supertrend1 && Close < Supertrend2 && MACD Histogram < 0`
- **Условия выхода**:
  - Лонг: `Close < Supertrend1 || Close < Supertrend2 || MACD Histogram < 0`
  - Шорт: `Close > Supertrend1 || Close > Supertrend2 || MACD Histogram > 0`
- **Стопы**: не используются.
- **Параметры по умолчанию**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `OscillatorMaType` = Exponential
  - `SignalMaType` = Exponential
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 20
  - `Factor2` = 5.0
  - `TradeDirection` = "Both"
- **Фильтры**:
  - Категория: следование тренду
  - Направление: настраиваемое
  - Индикаторы: Supertrend, MACD
  - Сложность: средняя
  - Уровень риска: средний
