# Стратегия Mikul's Ichimoku Cloud v2
[English](README.md) | [中文](README_cn.md)

Стратегия прорыва на основе облака Ишимоку с необязательным фильтром по скользящей средней. Позиции сопровождаются трейлинг-стопом (ATR, процент или правила Ишимоку) и опциональным тейк-профитом.

## Детали

- **Условия входа**: Пересечение Tenkan-sen выше Kijun-sen при цене выше облака или сильный пробой над зелёным облаком.
- **Лонг/Шорт**: Только лонг.
- **Условия выхода**: Трейлинг-стоп или разворот по Ишимоку, опциональный тейк-профит.
- **Стопы**: Трейлинг.
- **Значения по умолчанию**:
  - `TrailSource` = `LowsHighs`
  - `TrailMethod` = `Atr`
  - `TrailPercent` = 10
  - `SwingLookback` = 7
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1
  - `AddIchiExit` = false
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 25
  - `UseMaFilter` = false
  - `MaType` = `Ema`
  - `MaLength` = 200
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouBPeriod` = 52
  - `Displacement` = 26
  - `CandleType` = TimeSpan.FromHours(1)
- **Фильтры**:
  - Категория: Trend
  - Направление: Long
  - Индикаторы: Ichimoku, ATR
  - Стопы: Trailing
  - Сложность: Medium
  - Таймфрейм: Intraday (1h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Уровень риска: Medium
