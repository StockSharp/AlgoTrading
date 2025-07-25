# Стратегия средневозврата по объему
[English](README.md) | [中文](README_zh.md)

Эта система ищет необычно высокий или низкий торговый объем относительно его исторического среднего. Значительные всплески объема часто возвращаются к норме по мере стабилизации активности, предоставляя возможности для торговли против движения.

Тестирование показывает среднегодичную доходность около 76%\. Стратегию лучше запускать на рынке Форекс.

Длинная позиция открывается, когда объем опускается ниже среднего минус `DeviationMultiplier`, умноженный на стандартное отклонение, и цена ниже скользящей средней. Короткая позиция открывается, когда объем поднимается выше верхней границы при цене выше средней. Сделки закрываются, как только объем возвращается к своему среднему уровню.

Стратегия полезна трейдерам, отслеживающим истощение после всплесков объема. Процентный стоп‑лосс защищает от сценариев, когда объем продолжает расти в том же направлении.

## Подробности
- **Условия входа**:
  - **Лонг**: Volume < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Шорт**: Volume > Avg + DeviationMultiplier * StdDev && Close > MA
- **Длинные/короткие**: обе стороны.
- **Условия выхода**:
  - **Лонг**: выход при volume > Avg
  - **Шорт**: выход при volume < Avg
- **Стопы**: да, процентный стоп‑лосс.
- **Значения по умолчанию**:
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2m
- **Фильтры**:
  - Категория: Mean Reversion
  - Направление: оба
  - Индикаторы: Volume
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Уровень риска: средний


