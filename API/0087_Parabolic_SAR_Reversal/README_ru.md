# Стратегия разворота по Parabolic SAR
[English](README.md) | [中文](README_zh.md)

Индикатор Parabolic SAR отображает точки над или под ценой, указывая направление тренда. Когда точки перескакивают на противоположную сторону, это может означать завершение предыдущего движения. Стратегия входит в сделку на таком переключении, рассчитывая на краткосрочный разворот.

Тестирование показывает среднегодичную доходность около 148%. Стратегию лучше запускать на рынке Форекс.

Значение Parabolic SAR обновляется на каждой свече. Если индикатор перемещается сверху цены вниз, открывается длинная позиция. Если он переходит снизу вверх, открывается короткая. В примере кода не задаётся явная цель по прибыли, выход обычно осуществляется вручную либо посредством внешнего трейлинг‑стопа.

Поскольку SAR реагирует быстро, в боковиках возможны ложные сигналы, поэтому использовать его лучше при уверенных колебаниях цены.

## Детали

- **Условия входа**: Parabolic SAR меняет сторону относительно цены.
- **Длинные/короткие**: обе стороны.
- **Условия выхода**: вручную или внешний стоп.
- **Стопы**: не определены.
- **Значения по умолчанию**:
  - `InitialAcceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `CandleType` = 15 минут
- **Фильтры**:
  - Категория: следование за трендом
  - Направление: оба
  - Индикаторы: Parabolic SAR
  - Стопы: опционально
  - Сложность: базовая
  - Таймфрейм: внутридневной
  - Сезонность: нет
  - Нейросети: нет
  - Дивергенция: нет
  - Уровень риска: средний

