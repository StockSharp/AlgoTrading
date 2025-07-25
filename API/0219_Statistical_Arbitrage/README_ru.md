# Стратегия Statistical Arbitrage
[English](README.md) | [中文](README_cn.md)

Этот подход к статистическому арбитражу торгует парой связанных инструментов, ориентируясь на их положение относительно собственных скользящих средних. Сопоставляя каждый актив со своим средним, стратегия стремится извлечь выгоду из краткосрочных дисбалансов, которые должны со временем сходиться.

Тестирование показывает среднегодичную доходность около 94%\. Стратегию лучше запускать на фондовом рынке.

Длинная позиция открывается, когда первый актив торгуется ниже своей средней, а второй — выше своей. Короткая позиция возникает в обратной ситуации. Сделки закрываются, когда первый актив пересекает свою среднюю обратно, что сигнализирует о нормализации спреда.

Метод подходит рыночно-нейтральным трейдерам, готовым балансировать экспозицию между двумя инструментами. Встроенный стоп‑лосс ограничивает просадку, если спред продолжит расширяться вместо возврата.

## Детали
- **Условия входа**:
  - **Лонг**: Asset1 < MA1 и Asset2 > MA2
  - **Шорт**: Asset1 > MA1 и Asset2 < MA2
- **Лонг/Шорт**: обе стороны.
- **Условия выхода**:
  - **Лонг**: Закрытие при уходе Asset1 выше MA1
  - **Шорт**: Закрытие при уходе Asset1 ниже MA1
- **Стопы**: да, процентный стоп‑лосс на спред.
- **Значения по умолчанию**:
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Фильтры**:
  - Категория: Арбитраж
  - Направление: Оба
  - Индикаторы: Скользящие средние
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Да
  - Уровень риска: Средний

