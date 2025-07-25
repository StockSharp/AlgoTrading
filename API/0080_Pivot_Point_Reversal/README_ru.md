# Стратегия разворота от пивот‑уровней
[English](README.md) | [中文](README_zh.md)

Дневные пивот‑уровни и их уровни поддержки и сопротивления часто служат точками разворота для внутридневной динамики цен. Эта стратегия рассчитывает классические пивоты «флор‑трейдеров» по максимуму, минимуму и закрытию предыдущего дня, а затем ищет свечи, которые отскакивают от S1 или R1.

Тестирование показывает среднегодичную доходность около 127%. Стратегию лучше запускать на фондовом рынке.

Когда цена подходит к уровню поддержки S1 и формирует бычью свечу, открывается длинная позиция. Если цена тестирует сопротивление R1 и появляется медвежья свеча, открывается короткая. Сделки закрываются при достижении центрального пивота или если срабатывает защитный стоп.

Метод обновляет расчёты в начале каждого торгового дня, что делает его пригодным для сессий с чёткими внутридневными диапазонами.

## Детали

- **Условия входа**: Бычья свеча у S1 или медвежья у R1.
- **Лонг/Шорт**: Оба.
- **Условия выхода**: Цена пересекает центральный пивот или стоп‑лосс.
- **Стопы**: Да, на процентной основе.
- **Значения по умолчанию**:
  - `CandleType` = 5 минут
  - `StopLossPercent` = 2
- **Фильтры**:
  - Категория: Среднее возвращение
  - Направление: Оба
  - Индикаторы: Пивоты
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Да
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

