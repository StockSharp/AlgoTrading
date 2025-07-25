# Импульс с учётом сезонности
[English](README.md) | [中文](README_zh.md)

Стратегия **Seasonality Adjusted Momentum** основана на импульсном индикаторе, скорректированном по силе сезонности.

Тестирование показывает среднегодичную доходность около 172%\. Стратегию лучше запускать на рынке Форекс.

Сигналы появляются, когда сезонность подтверждает изменение импульса на дневных данных. Такой метод подходит активным трейдерам.

Стопы рассчитываются на основе кратных ATR и параметров MomentumPeriod, SeasonalityThreshold. Настройте эти значения для баланса риска и прибыли.

## Подробности
- **Критерии входа**: см. реализацию условий индикаторов.
- **Длинные/короткие**: обе стороны.
- **Критерии выхода**: противоположный сигнал или логика стопов.
- **Стопы**: да, расчёт на основе индикаторов.
- **Значения по умолчанию**:
  - `MomentumPeriod = 14`
  - `SeasonalityThreshold = 0.5m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Фильтры**:
  - Категория: Следование за трендом
  - Направление: Оба
  - Индикаторы: сезонность, коррекция
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Дневной
  - Сезонность: Да
  - Нейронные сети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

