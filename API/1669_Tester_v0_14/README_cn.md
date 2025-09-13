# Tester v0.14 策略

该示例策略是 MQL4 脚本 “Tester v0.14” 的简化移植，原策略针对 EURUSD 的 4 小时周期。

## 逻辑

- 计算 14 周期简单移动平均线和 MACD。
- 当收盘价高于 SMA 且 MACD 为正时买入。
- 当收盘价低于 SMA 且 MACD 为负时卖出。
- 开仓后在指定的 bar 数量后平仓。

本移植使用了 StockSharp 的高级 API，通过 `SubscribeCandles` 和 `Bind` 获取指标值。

## 参数

- **MinSignSum** – 开仓所需的最小信号数。
- **Risk** – 用于资金管理的账户风险比例。
- **TakeProfit / StopLoss** – 以点数表示的固定收益/止损。
- **BarsNumber** – 持仓的 bar 数量。
- **CandleType** – 使用的蜡烛类型（默认 4 小时）。

## 备注

原始 MQL 文件包含大量规则组合。本示例仅使用简化版本以说明结构。
