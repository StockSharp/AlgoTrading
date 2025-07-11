# Стратегия Volume Supertrend

Данная стратегия опирается на объём и индикатор Supertrend. Длинная позиция открывается, когда объём превышает средний уровень и цена находится над линией Supertrend. Короткая позиция берётся при повышенном объёме и цене ниже Supertrend. Подходит для торговли по тренду.

## Подробности
- **Условия входа**:
  - **Long**: Volume > Avg(Volume) && Price > Supertrend
  - **Short**: Volume > Avg(Volume) && Price < Supertrend
- **Long/Short**: обе стороны
- **Условия выхода**:
  - **Long**: закрытие позиции при развороте Supertrend вниз
  - **Short**: закрытие при развороте Supertrend вверх
- **Стопы**: да
- **Значения по умолчанию**:
  - `VolumeAvgPeriod` = 20
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Фильтры**:
  - Категория: Trend
  - Направление: оба
  - Индикаторы: Volume Supertrend
  - Стопы: да
  - Сложность: средняя
  - Таймфрейм: Intraday
  - Сезонность: нет
  - Нейронные сети: нет
  - Дивергенция: нет
  - Уровень риска: средний
