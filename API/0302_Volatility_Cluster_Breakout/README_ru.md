# Прорыв кластера волатильности
[English](README.md) | [中文](README_zh.md)

Стратегия **Volatility Cluster Breakout** основана на пробоях, возникающих при высоких кластерах волатильности.

Тестирование показывает среднегодичную доходность около 169%\. Стратегию лучше запускать на крипторынке.

Сигналы формируются, когда индикаторы подтверждают возможность пробоя на внутридневных данных (5м). Такой метод подходит активным трейдерам.

Стопы рассчитываются на основе кратных ATR и параметров PriceAvgPeriod, AtrPeriod. Настройте эти значения для баланса риска и прибыли.

## Подробности
- **Критерии входа**: см. реализацию условий индикаторов.
- **Длинные/короткие**: обе стороны.
- **Критерии выхода**: противоположный сигнал или логика стопов.
- **Стопы**: да, расчёт на основе индикаторов.
- **Значения по умолчанию**:
  - `PriceAvgPeriod = 20`
  - `AtrPeriod = 14`
  - `StdDevMultiplier = 2.0m`
  - `StopMultiplier = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Фильтры**:
  - Категория: Следование за трендом
  - Направление: Оба
  - Индикаторы: несколько индикаторов
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной (5м)
  - Сезонность: Нет
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

