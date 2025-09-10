# Стратегия BTC Chop Reversal
[English](README.md) | [中文](README_cn.md)

Эта стратегия торгует краткосрочные развороты по BTC, когда цена тестирует ATR-диапазоны и меняется импульс. Используются EMA, ATR, RSI, гистограмма MACD и фильтр всплесков объема.

## Детали

- **Условия входа**:
  - **Лонг**: `Low < EMA - ATR*Mult` && `RSI < Oversold` && `MACD hist rising` && `Close > Open` и нет всплеска продающего объема.
  - **Шорт**: `High > EMA + ATR*Mult` && `RSI > Overbought` && `MACD hist falling` && `Close < Open`.
- **Лонг/Шорт**: обе стороны.
- **Условия выхода**:
  - Позиции защищены тейк-профитом и стоп-лоссом.
- **Стопы**: тейк-профит 0.75%, стоп-лосс 0.4%.
- **Параметры по умолчанию**:
  - `EMA Period` = 23.
  - `ATR Length` = 55.
  - `ATR Multiplier` = 4.4.
  - `RSI Length` = 9.
  - `RSI Overbought` = 68.
  - `RSI Oversold` = 28.
  - `MACD Fast` = 14.
  - `MACD Slow` = 44.
  - `MACD Signal` = 3.
  - `Volume MA Length` = 16.
  - `Sell Spike Multiplier` = 1.5.
  - `Take Profit (%)` = 0.75.
  - `Stop Loss (%)` = 0.4.
- **Фильтры**:
  - Категория: Разворот.
  - Направление: обе.
  - Индикаторы: EMA, ATR, RSI, MACD, Volume.
  - Стопы: да.
  - Сложность: средняя.
  - Таймфрейм: краткосрочный.
  - Сезонность: нет.
  - Нейросети: нет.
  - Дивергенция: нет.
  - Уровень риска: средний.
