# MACD Sample Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the classic MetaTrader MACD Sample expert.
It uses a MACD cross combined with an EMA trend filter, separate take-profit and stop-loss levels for long and short trades, and an optional trailing stop. Trading is allowed only within a configurable time window.

## Details

- **Entry Criteria**:
  - **Long**: MACD line is below zero and crosses above the signal line while EMA is rising.
  - **Short**: MACD line is above zero and crosses below the signal line while EMA is falling.
- **Exit Criteria**:
  - Opposite MACD cross.
  - Reaching individual take-profit or stop-loss targets.
  - Trailing stop hit.
- **Long/Short**: Both.
- **Default Values**:
  - `EMA Period` = 26
  - `MACD Open Level` = 3
  - `MACD Close Level` = 2
  - `Take Profit Long` = 50
  - `Take Profit Short` = 75
  - `Stop Loss Long` = 80
  - `Stop Loss Short` = 50
  - `Trailing Stop` = 30
  - Trading hours: 4 to 19 UTC
- **Indicators**: MACD, EMA
- **Timeframe**: 1 hour candles by default

