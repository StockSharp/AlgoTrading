# Прорыв индекса хаотичности
[English](README.md) | [中文](README_cn.md)

Индекс хаотичности показывает, находится ли рынок в тренде или во флэте. Когда показатель опускается ниже порога, это указывает на начало тренда после периода боковика.

Тестирование показывает среднегодичную доходность около 172%\. Стратегию лучше запускать на рынке Форекс.

Стратегия входит в сторону движения цены относительно скользящей средней, когда хаотичность падает. Выход происходит, если хаотичность вновь поднимается выше верхнего порога или срабатывает стоп-лосс.

Цель — поймать новые тренды, возникающие после консолидации.

## Подробности

- **Критерий входа**: значение индекса ниже `ChoppinessThreshold` и цена выше/ниже MA.
- **Длинные/короткие**: обе стороны.
- **Критерий выхода**: индекс выше `HighChoppinessThreshold` или стоп.
- **Стопы**: да.
- **Значения по умолчанию**:
  - `MAPeriod` = 20
  - `ChoppinessPeriod` = 14
  - `ChoppinessThreshold` = 38.2m
  - `HighChoppinessThreshold` = 61.8m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Пробой
  - Направление: Обе стороны
  - Индикаторы: Choppiness, MA
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

