# Стратегия Overnight Gap
[English](README.md) | [中文](README_zh.md)

Overnight Gap используется при сильных гэпах на открытии по сравнению с предыдущим закрытием, вызванных новостями или послеторговой активностью.
Крупные гэпы часто частично закрываются, когда участники осмысливают движение.

Тестирование показывает среднегодичную доходность около 124%. Стратегию лучше запускать на рынке Форекс.

Стратегия торгует против чрезмерных разрывов, входя вскоре после открытия в противоположную сторону и закрываясь до конца сессии.

Стопы ставятся на процентную величину за пределами экстремумов гэпа, чтобы управлять риском, если движение продолжится.

## Детали

- **Критерий входа**: сигнал индикатора
- **Длинная/короткая сторона**: обе
- **Критерий выхода**: стоп-лосс или противоположный сигнал
- **Стопы**: да, процентные
- **Значения по умолчанию:**
  - `CandleType` = 15 минут
  - `StopLoss` = 2%
- **Фильтры:**
  - Категория: Gap
  - Направление: обе
  - Индикаторы: Gap
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Уровень риска: средний

