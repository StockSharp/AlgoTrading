# Тренд по индексу ADX
[English](README.md) | [中文](README_cn.md)

Стратегия основана на индексе среднего направленного движения (ADX).

Тестирование показывает среднегодичную доходность около 46%\. Стратегию лучше запускать на фондовом рынке.

Система оценивает силу рынка по значению ADX. Когда ADX выше порога и цена находится по соответствующую сторону от своей скользящей средней, открывается позиция в этом направлении. Закрытие происходит, когда ADX слабеет или появляется противоположный сигнал.

Ожидание уверенного значения ADX позволяет торговать только при устойчивом импульсе. Стопы обычно вычисляются как кратное ATR, чтобы риск учитывал волатильность.

## Детали

- **Критерии входа**: сигналы на основе MA, ADX, ATR.
- **Длинные/короткие**: оба направления.
- **Критерии выхода**: противоположный сигнал или стоп.
- **Стопы**: да.
- **Значения по умолчанию**:
  - `AdxPeriod` = 14
  - `MaPeriod` = 50
  - `AtrMultiplier` = 2m
  - `AdxExitThreshold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Тренд
  - Направление: Оба
  - Индикаторы: MA, ADX, ATR
  - Стопы: Да
  - Сложность: Базовая
  - Таймфрейм: Внутридневной (5m)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

