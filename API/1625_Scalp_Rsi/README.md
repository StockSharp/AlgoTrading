# Scalp RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Scalping strategy using rapid RSI changes. Converted from MetaTrader script `scalpen_rsi.mq4`.
The strategy opens trades when the RSI drops or rises sharply and applies fixed take-profit and stop-loss levels.

## Details

- **Entry Criteria**:
  - **Buy**: RSI value `buy_period` bars ago minus current RSI ≥ `BuyMovement`,
    previous RSI minus current RSI > `BuyBreakdown`, and current RSI < `BuyRsiValue`.
  - **Sell**: Current RSI minus RSI `sell_period` bars ago ≥ `SellMovement`,
    current RSI minus previous RSI > `SellBreakdown`, and current RSI > `SellRsiValue`.
- **Long/Short**: Both.
- **Exit Criteria**: Fixed take-profit and stop-loss in ticks.
- **Stops**: Yes, using `BuyStopLoss`, `BuyTakeProfit`, `SellStopLoss`, and `SellTakeProfit`.
- **Filters**:
  - Minimum delay between trades (`TradeDelaySeconds`).
  - Maximum simultaneous open trades (`MaxOpenTrades`).
