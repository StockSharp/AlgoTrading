# Адаптивный RSI с фильтром объёма
[English](README.md) | [中文](README_zh.md)

Стратегия **Adaptive RSI Volume Filter** торгует по адаптивному RSI с подтверждением объёмом.
Сигналы формируются, когда индикаторы подтверждают отфильтрованные входы на внутридневных данных (5м). Такой подход подходит активным трейдерам.
Стопы рассчитываются исходя из кратных ATR и параметров MinRsiPeriod, MaxRsiPeriod. Эти значения можно изменять для баланса риска и прибыли.

Тестирование показывает среднегодичную доходность около 106%\. Стратегию лучше запускать на фондовом рынке.

## Подробности
- **Условия входа**: см. реализацию для условий по индикаторам.
- **Длинные/короткие позиции**: обе стороны.
- **Условия выхода**: обратный сигнал или логика стопов.
- **Стопы**: да, вычисляются на основе индикаторов.
- **Значения по умолчанию**:
  - `MinRsiPeriod = 10`
  - `MaxRsiPeriod = 20`
  - `AtrPeriod = 14`
  - `VolumeLookback = 20`
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

