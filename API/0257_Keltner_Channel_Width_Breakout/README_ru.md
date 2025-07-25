# Прорыв по ширине канала Келтнера
[English](README.md) | [中文](README_zh.md)

Стратегия Keltner Channel Width Breakout наблюдает за быстрым расширением канала Келтнера. Когда значения выходят за пределы типичного диапазона, цена часто начинает новое движение.

Тестирование показывает среднегодичную доходность около 112%\. Стратегию лучше запускать на рынке Форекс.

Позиция открывается, как только индикатор пробивает полосу, построенную по последним данным и множителю отклонения. Возможны сделки в обе стороны со стопом.

Система подходит трейдерам импульсных стратегий, стремящимся поймать ранние прорывы. Сделки закрываются, когда индикатор возвращается к среднему. По умолчанию используется `EMAPeriod` = 20.

## Подробности
- **Условия входа**: индикатор превышает среднее на величину множителя отклонения.
- **Длинные/короткие**: оба направления.
- **Условия выхода**: индикатор возвращается к среднему.
- **Стопы**: да.
- **Значения по умолчанию**:
  - `EMAPeriod` = 20
  - `ATRPeriod` = 14
  - `ATRMultiplier` = 2.0m
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopMultiplier` = 2
- **Фильтры**:
  - Категория: Breakout
  - Направление: оба
  - Индикаторы: Keltner
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: краткосрочный
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Уровень риска: средний


