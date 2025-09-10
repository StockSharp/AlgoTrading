# BONK Long Volatility
[English](README.md) | [中文](README_cn.md)

Эта стратегия работает только в лонг и использует комбинацию скользящих средних, волатильности и объёмов. Покупка происходит при восходящем тренде, расширении диапазона и подтверждении моментума. Выход осуществляется по тейк-профиту, стоп-лоссу или трейлингу на базе ATR.

## Детали

- **Условия входа**: Быстрая MA выше медленной, диапазон свечи больше ATR * `AtrMultiplier`, RSI между `RsiOversold` и `RsiOverbought`, линия MACD выше сигнальной и нуля, объём выше SMA * `VolumeThreshold`, закрытие выше быстрой MA, свеча входит в последние `LookbackDays`.
- **Длинные/короткие позиции**: Только лонг.
- **Условия выхода**: Тейк-профит, стоп-лосс или ATR трейлинг.
- **Стопы**: Да.
- **Значения по умолчанию**:
  - `ProfitTargetPercent` = 5.0m
  - `StopLossPercent` = 3.0m
  - `AtrLength` = 10
  - `AtrMultiplier` = 1.5m
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeSmaLength` = 20
  - `VolumeThreshold` = 1.5m
  - `MaFastLength` = 5
  - `MaSlowLength` = 13
  - `LookbackDays` = 30
  - `CandleType` = TimeSpan.FromHours(1)
- **Фильтры**:
  - Категория: Trend
  - Направление: Long
  - Индикаторы: SMA, ATR, RSI, MACD, Volume
  - Стопы: Да
  - Сложность: Средняя
  - Таймфрейм: Внутридневной
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Уровень риска: Средний

