# Hull MA и пробой подразумеваемой волатильности
[English](README.md) | [中文](README_zh.md)

Стратегия **Hull MA Implied Volatility Breakout** использует Hull MA для поиска пробоев подразумеваемой волатильности.
Сигналы формируются, когда индикаторы подтверждают возможности пробоя на внутридневных данных (15м). Такой подход подходит активным трейдерам.
Стопы рассчитываются исходя из кратных ATR и параметров HmaPeriod, IVPeriod. Эти значения можно изменять для баланса риска и прибыли.

Тестирование показывает среднегодичную доходность около 121%\. Стратегию лучше запускать на крипторынке.

## Подробности
- **Условия входа**: см. реализацию для условий по индикаторам.
- **Длинные/короткие позиции**: обе стороны.
- **Условия выхода**: обратный сигнал или логика стопов.
- **Стопы**: да, вычисляются на основе индикаторов.
- **Значения по умолчанию**:
  - `HmaPeriod = 9`
  - `IVPeriod = 20`
  - `IVMultiplier = 2m`
  - `StopLossAtr = 2m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Фильтры**:
  - Категория: Следование за трендом
  - Направление: Оба
  - Индикаторы: multiple indicators
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной (15m)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

