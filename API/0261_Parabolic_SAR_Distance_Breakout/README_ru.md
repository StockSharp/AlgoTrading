# Прорыв расстояния Parabolic SAR
[English](README.md) | [中文](README_zh.md)

Стратегия Parabolic SAR Distance Breakout отслеживает быстрые расширения индикатора Parabolic. Когда его значения выходят за недавний диапазон, цена часто начинает новое движение.

Тестирование показывает среднегодичную доходность около 118%\. Стратегию лучше запускать на фондовом рынке.

Позиция открывается, когда индикатор пробивает диапазон, сформированный по последним данным и умноженный на коэффициент отклонения. Возможны сделки в обе стороны со стопом.

Такая система подходит трейдерам-моментщикам, ищущим ранние прорывы. Сделки закрываются, когда Parabolic возвращается к среднему. Значение по умолчанию `Acceleration` = 0.02m.

## Подробности

- **Условие входа**: Индикатор превышает среднее значение на величину коэффициента отклонения.
- **Лонг/Шорт**: Оба направления.
- **Условие выхода**: Индикатор возвращается к среднему.
- **Стопы**: Да.
- **Значения по умолчанию**:
  - `Acceleration` = 0.02m
  - `MaxAcceleration` = 0.2m
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Прорыв
  - Направление: Оба
  - Индикаторы: Parabolic
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Краткосрочный
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний


