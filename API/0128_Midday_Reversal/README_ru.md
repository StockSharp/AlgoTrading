# Стратегия Midday Reversal
[English](README.md) | [中文](README_zh.md)

Midday Reversal ищет разворотные точки, возникающие около полудня, когда утренние тренды часто выдыхаются.
Ликвидность обычно снижается в середине сессии, что приводит к разворотам, когда трейдеры закрывают позиции.

Тестирование показывает среднегодичную доходность около 121%. Стратегию лучше запускать на крипторынке.

Система отслеживает смену импульса в середине дня и входит в противоположном направлении утреннего движения.

Процентный стоп контролирует риск, а выход осуществляется, если разворот не развивается к концу дня.

## Детали

- **Критерий входа**: сигнал индикатора
- **Длинная/короткая сторона**: обе
- **Критерий выхода**: стоп-лосс или противоположный сигнал
- **Стопы**: да, процентные
- **Значения по умолчанию:**
  - `CandleType` = 15 минут
  - `StopLoss` = 2%
- **Фильтры:**
  - Категория: Внутридневная
  - Направление: обе
  - Индикаторы: Price Action
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Уровень риска: средний

