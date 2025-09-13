# EA Template Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy originates from a MetaTrader EA template. It analyses the previous finished candle and opens a position in the direction of the candle body. A bullish candle triggers a long trade, while a bearish candle triggers a short one. Reverse mode flips the interpretation of the candle so the strategy trades against the bar color.

The strategy supports fixed position size or an equity based calculation. Stop-loss and take-profit levels are set in points from the entry price. Trading is skipped when the spread exceeds the allowed threshold.

## Details

- **Entry Criteria**:
  - **Long**: previous candle close > open and `ReverseTrade` disabled.
  - **Short**: previous candle close < open and `ReverseTrade` disabled.
  - When `ReverseTrade` is enabled the signals are inverted.
  - Spread must be below `SpreadLimit` points.
- **Exit Criteria**:
  - Opposite candle color or stop-loss/take-profit hit.
- **Position Sizing**:
  - Fixed size `Lots` or equity based size using `RiskPercent` when `UseMoneyManagement` is true.
- **Stops**:
  - `StopLoss` and `TakeProfit` in points relative to entry price.
- **Long/Short**: Both directions.
- **Indicators**: None.
- **Risk Level**: Medium.

Parameters allow tuning candle type, reverse mode, money management rules and risk limits.
