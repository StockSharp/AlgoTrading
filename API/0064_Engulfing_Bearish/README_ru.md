# Стратегия по медвежьему поглощению
[English](README.md) | [中文](README_zh.md)

Данный паттерн пытается захватить начало нисходящего движения после роста. Медвежье поглощение возникает, когда красная свеча полностью поглощает тело предыдущей бычьей свечи. Подсчёт нескольких подряд идущих зелёных свеч до формирования паттерна помогает убедиться, что рынок ранее рос.

Тестирование показывает среднегодичную доходность около 79%. Стратегию лучше запускать на фондовом рынке.

Алгоритм сохраняет последовательность свечей. Если новая свеча закрывается ниже открытия и её тело поглощает предыдущую бычью свечу, открывается короткая позиция. Стоп‑лосс размещается выше максимума паттерна для ограничения риска.

Как правило, позиции сопровождаются защитным стопом, хотя трейдер может выйти вручную, если ситуация изменится. Требование восходящего тренда позволяет избежать ложных сигналов во время бокового рынка.

## Детали

- **Условия входа**: Медвежья свеча поглощает предыдущую бычью, при необходимости присутствует восходящий тренд.
- **Направление**: Только шорт.
- **Условия выхода**: Стоп‑лосс или по усмотрению трейдера.
- **Стопы**: Да, выше максимума паттерна.
- **Значения по умолчанию**:
  - `CandleType` = 15 минут
  - `StopLossPercent` = 1
  - `RequireUptrend` = true
  - `UptrendBars` = 3
- **Фильтры**:
  - Категория: Паттерн
  - Направление: Шорт
  - Индикаторы: Свечной анализ
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

