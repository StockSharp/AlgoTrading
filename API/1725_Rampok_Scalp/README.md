# Rampok Scalp Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Scalping system that trades when price breaks moving average envelopes.
The strategy enters long when price crosses above the lower band and
short when price crosses below the upper band. Positions are protected
by optional take-profit, stop-loss and trailing stop parameters.

## Details

- **Entry Criteria**:
  - **Buy**: previous close below lower band and current close above it.
  - **Sell**: previous close above upper band and current close below it.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Take profit, stop loss or trailing stop.
- **Stops**: Configurable SL/TP and trailing.
- **Filters**: none.
