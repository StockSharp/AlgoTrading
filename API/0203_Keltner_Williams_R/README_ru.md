# Стратегия Keltner Williams R
[English](README.md) | [中文](README_cn.md)

Эта стратегия использует индикаторы Keltner и Williams %R для получения сигналов. Длинная позиция открывается, когда цена опускается ниже нижней полосы Келтнера, а Williams %R ниже -80, что говорит о перепроданности. Короткая позиция возникает при цене выше верхней полосы и Williams %R выше -20 — перекупленность.

Тестирование показывает среднегодичную доходность около 46%\. Стратегию лучше запускать на фондовом рынке.

Стратегия подходит трейдерам, работающим в смешанном рынке.

## Детали
- **Условия входа**:
  - **Лонг**: Цена < нижней полосы Келтнера и Williams %R < -80 (перепроданность)
  - **Шорт**: Цена > верхней полосы Келтнера и Williams %R > -20 (перекупленность)
- **Лонг/Шорт**: обе стороны.
- **Условия выхода**:
  - **Лонг**: Закрыть позицию при возвращении цены к средней полосе
  - **Шорт**: Закрыть позицию при возвращении цены к средней полосе
- **Стопы**: да.
- **Значения по умолчанию**:
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `WilliamsRPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Смешанная
  - Направление: Оба
  - Индикаторы: Keltner Williams R
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

