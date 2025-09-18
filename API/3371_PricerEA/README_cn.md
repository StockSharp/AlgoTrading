# PricerEA 策略

## 概述

**PricerEA Strategy** 使用 StockSharp 高级 API 复刻 MetaTrader 4 专家顾问“PricerEA v1.0”。策略会在用户指定的价格
处放置多达四个挂单（Buy Stop、Sell Stop、Buy Limit、Sell Limit）。任意挂单成交后，会自动附加止损、止盈，并可选启用
移动止损和保本逻辑，以保持与原始 EA 一致。

## 运行流程

1. **挂单** – 启动时根据输入的绝对价格只发送一次挂单，可选设置有效期（分钟）。
2. **手数** – 可使用固定手数，也可启用自动模式，根据投资组合余额和 Risk Factor 参数计算手数。
3. **保护单** – 成交后按照配置的点数距离（以 `Security.PriceStep` 计）下达止损和止盈。若同时启用移动止损与保本，
   止损会在价格走出“保本触发距离 + 初始止损距离”后再移动，完全复现 MQL 逻辑。
4. **挂单维护** – 到期或策略停止时自动撤销剩余挂单。

## 参数说明

| 参数 | 描述 |
|------|------|
| `BuyStopPrice`, `SellStopPrice`, `BuyLimitPrice`, `SellLimitPrice` | 对应挂单的绝对价格，填 `0` 表示不下单。 |
| `TakeProfitPoints` | 止盈距离，单位为价格点 (`Security.PriceStep`)。 |
| `StopLossPoints` | 止损距离，单位为价格点。 |
| `EnableTrailingStop` | 是否启用移动止损。 |
| `TrailingStepPoints` | 每次移动止损所需的最小价格改善（点）。 |
| `EnableBreakEven` | 是否启用保本逻辑。 |
| `BreakEvenTriggerPoints` | 保本前需要的额外利润（点）。 |
| `PendingExpiryMinutes` | 挂单有效期（分钟），填 `0` 表示无限期。 |
| `VolumeMode` | 手数模式：手动或自动。 |
| `RiskFactor` | 自动模式下使用的风险乘数，对应 MT4 参数。 |
| `ManualVolume` | 手动模式下使用的固定手数。 |

## 与 MT4 版本的差异

- 自动手数使用 StockSharp 投资组合余额和合约乘数计算，结果可能与 MetaTrader 略有差异。
- 止损/止盈通过 StockSharp 辅助方法下单，会自动遵循交易所的手数步长、最小与最大手数限制。
- 挂单有效期由策略内部控制，而 MT4 依赖服务器端的到期机制。

## 使用提示

- 启动前设置好各个价格层级，值为 0 的订单不会创建。
- 所有以“点”为单位的参数均基于 `Security.PriceStep`，等价于 MT4 中对 `Digits` 的处理。
- 建议结合 StockSharp 的组合管理和日志功能，监控挂单与保护单的状态。
