# Dual Strategy Selector V2 - Cryptogyani Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy switches between two SMA-based long-only approaches.

- **Strategy 1**: trades SMA crossover with optional trailing take profit or fixed target.
- **Strategy 2**: trades SMA crossover confirmed by a higher timeframe trend, uses ATR stop and partial take profit.

## Details

- **Entry Criteria**:
  - Strategy 1: fast SMA crosses above slow SMA.
  - Strategy 2: fast SMA crosses above slow SMA and price is above higher timeframe SMA.
- **Exit Criteria**:
  - Strategy 1: take profit target or trailing stop.
  - Strategy 2: partial take profit then ATR-based stop.
- **Indicators**: SMA, ATR.
- **Direction**: Long only.
- **Stops**: Yes.
