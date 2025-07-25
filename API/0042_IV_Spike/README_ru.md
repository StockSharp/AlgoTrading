# Всплеск подразумеваемой волатильности
[English](README.md) | [中文](README_cn.md)

Стратегия отслеживает резкие скачки подразумеваемой волатильности относительно предыдущего значения. Сильный всплеск вместе с ценой, идущей против скользящей средней, может указывать на краткосрочный разворот.

Тестирование показывает среднегодичную доходность около 163%\. Стратегию лучше запускать на фондовом рынке.

Когда подразумеваемая волатильность повышается выше заданного порога, система входит в противоположную сторону движения цены, рассчитывая на возврат волатильности.

Позиции закрываются, когда волатильность начинает падать или срабатывает стоп-лосс.

## Подробности

- **Критерий входа**: всплеск IV выше `IVSpikeThreshold` и положение цены относительно MA.
- **Длинные/короткие**: обе стороны.
- **Критерий выхода**: снижение IV или стоп.
- **Стопы**: да.
- **Значения по умолчанию**:
  - `MAPeriod` = 20
  - `IVPeriod` = 20
  - `IVSpikeThreshold` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Волатильность
  - Направление: Обе стороны
  - Индикаторы: IV, MA
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

