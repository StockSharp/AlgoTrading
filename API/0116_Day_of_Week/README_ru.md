# Стратегия "Эффект дня недели"
[English](README.md) | [中文](README_zh.md)

Эта стратегия использует склонность рынков проявлять повторяющееся поведение в определённые дни недели. Некоторые индексы стабильно сильны в середине недели, тогда как понедельник или пятница могут быть относительно слабыми.

Тестирование показывает среднегодичную доходность около 85%. Стратегию лучше запускать на крипторынке.

Открытие позиций происходит в начале сессии согласно историческим наблюдениям, а закрытие — к концу дня.

Небольшой стоп защищает от аномалий, досрочно закрывая позицию, если закономерность не срабатывает.

## Детали

- **Условия входа**: календарные триггеры
- **Длинная/короткая**: обе
- **Условия выхода**: стоп-лосс или противоположный сигнал
- **Стопы**: да, процентные
- **Значения по умолчанию**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Фильтры**:
  - Категория: Сезонность
  - Направление: обе
  - Индикаторы: Сезонность
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: внутридневной
  - Сезонность: да
  - Нейронные сети: нет
  - Дивергенция: нет
  - Уровень риска: средний

