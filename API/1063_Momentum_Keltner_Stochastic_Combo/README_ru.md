# Стратегия Momentum Keltner Stochastic Combo
[English](README.md) | [中文](README_cn.md)

Стратегия объединяет сравнение момента цены со стохастиком на базе каналов Кельтнера.  
Размер позиции динамически увеличивается в зависимости от эквити и защищается фиксированным стоп-лоссом.

## Детали

- **Критерии входа**:  
  - Лонг: `Momentum > 0` и `KeltnerStoch < Threshold`  
  - Шорт: `Momentum < 0` и `KeltnerStoch > Threshold`
- **Лонг/Шорт**: Оба  
- **Критерии выхода**:  
  - Лонг: `KeltnerStoch > Threshold`  
  - Шорт: `KeltnerStoch < Threshold`
- **Стопы**: фиксированный `SlPoints` от цены входа  
- **Значения по умолчанию**:  
  - `MomLength` = 7  
  - `KeltnerLength` = 9  
  - `KeltnerMultiplier` = 0.5  
  - `Threshold` = 99  
  - `AtrLength` = 20  
  - `SlPoints` = 1185  
  - `EnableScaling` = true  
  - `BaseContracts` = 1  
  - `InitialCapital` = 30000  
  - `EquityStep` = 150000  
  - `MaxContracts` = 15  
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Фильтры**:  
  - Категория: Следование тренду  
  - Направление: Оба  
  - Индикаторы: Momentum, EMA, ATR  
  - Стопы: Да  
  - Сложность: Средняя  
  - Таймфрейм: Среднесрочный  
  - Сезонность: Нет  
  - Нейросети: Нет  
  - Дивергенция: Нет  
  - Уровень риска: Средний

