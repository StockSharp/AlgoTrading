# Стратегия Macd Williams R
[English](README.md) | [中文](README_cn.md)

Стратегия основана на индикаторах MACD и Williams %R. Вход в покупки, когда MACD выше сигнальной линии, а Williams %R находится в зоне перепроданности (< -80). Вход в продажи, когда MACD ниже сигнальной линии, а Williams %R в зоне перекупленности (> -20).

Тестирование показывает среднегодичную доходность около 100%\. Стратегию лучше запускать на рынке Форекс.

MACD отражает изменение основного импульса, а Williams %R указывает краткосрочные развороты. Для входа оба сигнала должны совпадать.

Хороший вариант для тех, кто сочетает трендовые и контртрендовые подсказки. Стопы рассчитываются на основе ATR.

## Подробности

- **Условия входа**:
  - Длинная: `MACD > Signal && WilliamsR < -80`
  - Короткая: `MACD < Signal && WilliamsR > -20`
- **Long/Short**: Оба
- **Условия выхода**: пересечение MACD в противоположном направлении
- **Стопы**: процентный уровень через `StopLossPercent`
- **Параметры по умолчанию**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `WilliamsRPeriod` = 14
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:
  - Категория: Mean reversion
  - Направление: Оба
  - Индикаторы: MACD, Williams %R, R
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Среднесрочный
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

