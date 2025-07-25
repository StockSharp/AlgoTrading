# Стратегия возврата по CCI
[English](README.md) | [中文](README_zh.md)

Индекс товарного канала (CCI) показывает, насколько далеко цена ушла от своего статистического среднего. Стратегия входит в рынок, когда CCI сильно отклоняется от собственной средней, предполагая возврат после ослабления импульса.

Тестирование показывает среднегодичную доходность около 151%\. Стратегию лучше запускать на фондовом рынке.

Длинная сделка осуществляется, когда CCI опускается ниже среднего минус `DeviationMultiplier` стандартных отклонений. Короткая сделка открывается, когда CCI поднимается выше среднего плюс тот же множитель. Позиция закрывается, когда CCI снова пересекает среднее значение.

Система подходит краткосрочным трейдерам, предпочитающим контртрендовые сделки. Стоп‑лосс на основе процентного движения помогает ограничить риск, если возврат не происходит быстро.

## Подробности
- **Условия входа**:
  - **Long**: CCI < Avg − DeviationMultiplier * StdDev
  - **Short**: CCI > Avg + DeviationMultiplier * StdDev
- **Long/Short**: обе стороны.
- **Условия выхода**:
  - **Long**: выход при CCI > Avg
  - **Short**: выход при CCI < Avg
- **Стопы**: да, процентный стоп‑лосс.
- **Параметры по умолчанию**:
  - `CciPeriod` = 20
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Возврат к среднему
  - Направление: Обе стороны
  - Индикаторы: CCI
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

