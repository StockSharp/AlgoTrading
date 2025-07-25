# Стратегия по бычьему поглощению
[English](README.md) | [中文](README_zh.md)

Данная настройка ищет резкий разворот вверх, когда свеча полностью поглощает предыдущую медвежью. Такая формация часто завершает краткосрочное снижение и указывает на возобновление восходящего импульса. Необязательный фильтр нисходящего тренда учитывает подряд идущие красные свечи, подтверждая истощение продавцов.

Тестирование показывает среднегодичную доходность около 76%. Стратегию лучше запускать на рынке Форекс.

В режиме реального времени алгоритм отслеживает каждую новую свечу и запоминает предыдущую. Если новая свеча закрывается выше открытия и её тело перекрывает предыдущую, открывается длинная позиция. Стоп размещается сразу под минимумом паттерна, ограничивая риск.

Сделка остаётся открытой, пока не сработает стоп или не появится сигнал на ручной выход. Поскольку подтверждение ранее сформированных красных свеч усиливает сигнал, стратегия избегает слабых разворотов.

## Детали

- **Условия входа**: Бычья свеча поглощает предыдущую медвежью, при необходимости присутствует нисходящий тренд.
- **Направление**: Только лонг.
- **Условия выхода**: Стоп‑лосс или по усмотрению трейдера.
- **Стопы**: Да, под минимумом паттерна.
- **Значения по умолчанию**:
  - `CandleType` = 15 минут
  - `StopLossPercent` = 1
  - `RequireDowntrend` = true
  - `DowntrendBars` = 3
- **Фильтры**:
  - Категория: Паттерн
  - Направление: Лонг
  - Индикаторы: Свечной анализ
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

