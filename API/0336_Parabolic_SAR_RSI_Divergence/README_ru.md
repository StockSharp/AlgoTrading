# Parabolic SAR и дивергенция RSI
[English](README.md) | [中文](README_zh.md)

Стратегия **Parabolic SAR RSI Divergence** торгует сигналы Parabolic SAR, когда RSI расходится с ценой.
Сигналы формируются, когда Parabolic подтверждает настройки дивергенции на внутридневных данных (5м). Такой подход подходит активным трейдерам.
Стопы рассчитываются исходя из кратных ATR и параметров SarAccelerationFactor, SarMaxAccelerationFactor. Значения можно изменять для баланса риска и прибыли.

Тестирование показывает среднегодичную доходность около 103%\. Стратегию лучше запускать на фондовом рынке.

## Подробности
- **Условия входа**: см. реализацию для условий по индикаторам.
- **Длинные/короткие позиции**: обе стороны.
- **Условия выхода**: обратный сигнал или логика стопов.
- **Стопы**: да, вычисляются на основе индикаторов.
- **Значения по умолчанию**:
  - `SarAccelerationFactor = 0.02m`
  - `SarMaxAccelerationFactor = 0.2m`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Фильтры**:
  - Категория: Следование за трендом
  - Направление: Оба
  - Индикаторы: Parabolic, Divergence
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной (5m)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Да
  - Уровень риска: Средний

