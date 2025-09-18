# Dealers Trade MACD MQL4 策略
[English](README.md) | [Русский](README_ru.md)

Dealers Trade MACD MQL4 策略是 MetaTrader 4 上 "Dealers Trade v7.74" 智能交易系统的完整移植版本。该实现保留了原策略的金字塔加仓、MACD 斜率判别以及账户保护机制，并针对 StockSharp 的净持仓模式重新整理了仓位管理，适合在 H4 和 D1 等中长周期图表上进行波段交易。

## 策略工作流程

- **信号判定**：订阅指定周期的 K 线并计算经典 MACD（快 EMA、慢 EMA、信号 EMA）。当当前柱的 MACD 主线高于上一柱时视为看多动能；低于上一柱时视为看空动能。`ReverseCondition` 参数可用于反转信号方向。
- **加仓间距**：任一时刻仅维护一个方向的订单篮子。若 MACD 给出做多信号，会先发送基础 Buy 市价单；之后只有在价格相对上一单下移至少 `SpacingPips * PriceStep` 时才会继续买入，从而复制原脚本的“摊低成本”行为。做空逻辑对称处理。
- **手数控制**：基础手数取 `FixedVolume`，或者在启用 `UseRiskSizing` 时根据账户权益和 `RiskPercent` 计算。`IsStandardAccount` 用于区分标准账户与迷你账户（迷你账户手数缩小 10 倍）。每增加一单都会乘以 `LotMultiplier`，并受 `MaxVolume` 限制。
- **风险管理**：按照 `StopLossPips` 和 `TakeProfitPips` 为每笔订单设置固定止损止盈。盈利达到 `TrailingStopPips + SpacingPips` 时，止损会沿趋势方向上移/下移 `TrailingStopPips`，模拟原策略的移动止损。
- **账户保护**：当打开的订单数达到 `MaxTrades - OrdersToProtect`，且总体浮动盈利超过 `SecureProfit`，策略会平掉最新的仓位以锁定利润。这一逻辑与 MQL4 版本中的 AccountProtection 模块一致。

## 参数

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | H4 | 用于计算 MACD 的 K 线周期。 |
| `FixedVolume` | 0.1 | 未启用风险管理时使用的基础手数。 |
| `UseRiskSizing` | true | 是否根据账户权益计算手数。 |
| `RiskPercent` | 2 | 启用风险管理时的风险占比。 |
| `IsStandardAccount` | true | 标准账户为 true；迷你账户设为 false（手数 ÷10）。 |
| `MaxVolume` | 5 | 单笔订单允许的最大手数。 |
| `LotMultiplier` | 1.5 | 同一篮子中每增加一单的手数乘数。 |
| `MaxTrades` | 5 | 同时持有的最大订单数量。 |
| `SpacingPips` | 4 | 相邻订单之间的最小点差。 |
| `OrdersToProtect` | 3 | 启动账户保护前保留的订单数量。 |
| `AccountProtection` | true | 是否启用账户保护逻辑。 |
| `SecureProfit` | 50 | 触发保护所需的浮动盈利（账户货币）。 |
| `TakeProfitPips` | 30 | 每笔订单的止盈点数。 |
| `StopLossPips` | 90 | 每笔订单的止损点数。 |
| `TrailingStopPips` | 15 | 移动止损的固定距离。 |
| `ReverseCondition` | false | 反转 MACD 斜率的方向判定。 |
| `MacdFast` | 14 | MACD 快速 EMA 周期。 |
| `MacdSlow` | 26 | MACD 慢速 EMA 周期。 |
| `MacdSignal` | 1 | MACD 信号 EMA 周期。 |

## 注意事项

- StockSharp 采用净持仓模式，因此无法同时持有同一品种的多头和空头篮子；切换方向时会先关闭原方向仓位。
- 账户保护计算浮盈时使用 `PriceStep` 和 `StepPrice`。若证券未提供这类元数据，将回退到 0.0001 的点值与 1 的价格步长，必要时请调整阈值。
- 启用风险管理时必须提供正值的 `StopLossPips`；若止损为 0，将无法计算手数并跳过下单。
- 策略只在收盘后评估信号。与 MQL4 中的逐笔运算相比，部分信号可能延后一根 K 线出现，但回测表现更加稳定。
