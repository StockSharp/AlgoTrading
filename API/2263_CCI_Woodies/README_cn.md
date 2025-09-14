# CCI Woodies 策略

## 概述
该策略基于 Woodies CCI 方法的两条 CCI 线（快线和慢线）的交叉进行交易。在指定的时间框架上计算两条 CCI。当快线从上向下穿过慢线时，策略开多仓并平掉可能存在的空仓；当快线从下向上穿过慢线时，策略开空仓并平掉可能存在的多仓。

## 参数
- **FastPeriod**：快速 CCI 的周期。
- **SlowPeriod**：慢速 CCI 的周期。
- **CandleType**：用于计算的 K 线类型（时间框架）。
- **InvertSignals**：为 `true` 时，买卖逻辑反向。
- **TakeProfitPoints**：以价格点表示的止盈。
- **StopLossPoints**：以价格点表示的止损。

## 说明
策略使用 StockSharp 的高级 API，通过 `SubscribeCandles` 订阅 K 线，并通过 `Bind` 绑定指标。止盈和止损由 `StartProtection` 管理。
