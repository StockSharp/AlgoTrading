# Scalp RSI 策略
[English](README.md) | [Русский](README_ru.md)

基于 RSI 快速变化的剥头皮策略，来源于 MetaTrader 脚本 `scalpen_rsi.mq4`。
当 RSI 快速下跌或上升时开仓，并使用固定的止盈止损。

## 细节

- **入场条件**：
  - **买入**：`buy_period` 根K线前的 RSI 与当前值差值 ≥ `BuyMovement`，
    前一根K线的 RSI 与当前值差值 > `BuyBreakdown`，且当前 RSI < `BuyRsiValue`。
  - **卖出**：当前 RSI 与 `sell_period` 根K线前的 RSI 差值 ≥ `SellMovement`，
    当前 RSI 与前一根K线 RSI 差值 > `SellBreakdown`，且当前 RSI > `SellRsiValue`。
- **多空方向**：双向。
- **离场条件**：固定止盈与止损（tick）。
- **止损**：使用 `BuyStopLoss`、`BuyTakeProfit`、`SellStopLoss`、`SellTakeProfit`。
- **过滤器**：
  - 交易间的最小间隔 (`TradeDelaySeconds`)。
  - 同时持有的最大仓位数 (`MaxOpenTrades`)。
