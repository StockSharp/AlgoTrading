# Кластер объёма MACD
[English](README.md) | [中文](README_zh.md)

Стратегия **MACD Volume Cluster** построена на анализе объёмных кластеров MACD.
Сигналы формируются, когда индикаторы подтверждают смену тренда на внутридневных данных (5м). Такой подход подходит активным трейдерам.
Стопы рассчитываются исходя из кратных ATR и параметров FastMacdPeriod, SlowMacdPeriod. Эти значения можно изменять для баланса риска и прибыли.

## Подробности
- **Условия входа**: см. реализацию для условий по индикаторам.
- **Длинные/короткие позиции**: обе стороны.
- **Условия выхода**: обратный сигнал или логика стопов.
- **Стопы**: да, вычисляются на основе индикаторов.
- **Значения по умолчанию**:
  - `FastMacdPeriod = 12`
  - `SlowMacdPeriod = 26`
  - `MacdSignalPeriod = 9`
  - `VolumePeriod = 20`
  - `VolumeDeviationFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Фильтры**:
  - Категория: Следование за трендом
  - Направление: Оба
  - Индикаторы: multiple indicators
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной (5m)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний
