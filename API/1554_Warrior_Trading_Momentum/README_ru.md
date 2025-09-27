# Warrior Trading Momentum Strategy
[English](README.md) | [中文](README_cn.md)

Стратегия импульсной торговли в стиле Warrior Trading, объединяющая поиск разрывов, VWAP и паттерн "из красного в зелёное".

## Детали

- **Условия входа**: Gap-and-go, red-to-green или отскок от VWAP с объёмом.
- **Длинные/короткие**: Только лонг.
- **Условия выхода**: Стоп и тейк на основе ATR, с трейлингом.
- **Стопы**: Да.
- **Значения по умолчанию**:
  - `GapThreshold` = 2m
  - `GapVolumeMultiplier` = 2m
  - `VwapDistance` = 0.5m
  - `MinRedCandles` = 3
  - `RiskRewardRatio` = 2m
  - `TrailingStopTrigger` = 1m
  - `MaxDailyTrades` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Фильтры**:
  - Категория: Импульс
  - Направление: Лонг
  - Индикаторы: VWAP, RSI, EMA, ATR, Volume
  - Стопы: Да
  - Сложность: Продвинутая
  - Таймфрейм: Внутридневной (1м)
  - Сезонность: Нет
  - Нейросети: Нет
  - Дивергенция: Нет
  - Риск: Высокий
