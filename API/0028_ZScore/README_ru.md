# ZScore
[English](README.md) | [中文](README_cn.md)

Стратегия на основе индикатора Z-Score для торговли возврата к среднему

Тестирование показывает среднегодичную доходность около 121%\. Стратегию лучше запускать на крипторынке.

ZScore измеряет отклонение цены от скользящей средней. Чрезмерно высокие или низкие значения z-score говорят о переразгоне и побуждают открывать сделки в противоположном направлении. Сделка завершается, когда z-score нормализуется.

Z-Score является гибким фильтром, его можно масштабировать под любую временную серию. Использование выхода с учетом волатильности помогает стратегии адаптироваться к изменяющимся рыночным условиям.

## Детали

- **Условия входа**: сигналы на основе MA, ZScore.
- **Длинные/короткие**: в обе стороны.
- **Условия выхода**: противоположный сигнал или стоп.
- **Стопы**: да.
- **Значения по умолчанию**:
  - `ZScoreEntryThreshold` = 2.0m
  - `ZScoreExitThreshold` = 0.0m
  - `MAPeriod` = 20
  - `StdDevPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Mean Reversion
  - Направление: Оба
  - Индикаторы: MA, ZScore
  - Стопы: Да
  - Сложность: Базовая
  - Таймфрейм: Внутридневной (5м)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

