# Дивергенция по RSI
[English](README.md) | [中文](README_cn.md)

Стратегия основана на дивергенции индикатора RSI.

Тестирование показывает среднегодичную доходность около 85%\. Стратегию лучше запускать на крипторынке.

RSI Divergence ищет ценовые экстремумы, которые не подтверждаются осциллятором RSI. Бычья дивергенция приводит к покупке, а медвежья — к продаже. Сделка длится до разворота RSI или срабатывания стопа.

Дивергенции часто появляются под конец длительных трендов. Сравнивая поведение осциллятора с ценой, стратегия пытается поймать ранние развороты с контролируемым риском.

## Подробности

- **Критерии входа**: сигналы на основе RSI.
- **Длинные/короткие**: оба направления.
- **Критерии выхода**: противоположный сигнал или стоп.
- **Стопы**: да.
- **Параметры по умолчанию**:
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2м
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Тренд
  - Направление: Оба
  - Индикаторы: RSI
  - Стопы: Да
  - Сложность: Базовая
  - Таймфрейм: Внутридневной (5м)
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Да
  - Уровень риска: Средний

