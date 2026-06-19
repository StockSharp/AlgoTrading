# Target EA 管理策略

## 概述
**Target EA 管理策略** 是 MetaTrader 专家顾问 *TargetEA_v1.5* 的 StockSharp 版本。该策略不会主动开仓，而是持续监控当前属于策略的订单的浮动盈亏，并在达到用户设定的阈值时平掉仓位、撤销挂单。策略完整保留了原始 EA 的“篮子”管理模式：买单和卖单既可以分别评估，也可以作为单一篮子整体处理。

策略订阅 Level1（买一 / 卖一）行情，并使用高级 API 完成平仓与撤单。最新的买价和卖价会被转换成当前持仓的浮动盈亏指标。

## 核心特性
- **独立或合并篮子** —— 通过 `ManageBuySellOrders` 设置买单和卖单分别处理还是合并处理。
- **多种目标类型** —— 支持按点数、按每手货币金额、按账户余额百分比触发，与 MQL 中的 `TypeTargetUse` 一致。
- **盈亏双触发** —— `CloseInProfit` 和 `CloseInLoss` 分别控制浮盈或浮亏触发时是否平仓。
- **挂单清理** —— 在篮子被关闭后，可选择自动撤销对应方向的挂单。
- **高级 API 操作** —— 使用 `BuyMarket` / `SellMarket` 完成市价平仓，并直接基于策略订单集合撤销挂单。

## 参数说明
| 参数 | 描述 |
|------|------|
| `ManageBuySellOrders` | `Separate` 表示多头和空头分别成篮，`Combined` 表示合并评估。 |
| `CloseBuyOrders` / `CloseSellOrders` | 允许在满足条件时平掉相应方向的持仓。 |
| `DeleteBuyPendingPositions` / `DeleteSellPendingPositions` | 篮子平仓后撤销相应方向的挂单。 |
| `TypeTargetUse` | 选择按 `Pips`、`CurrencyPerLot` 或 `PercentageOfBalance` 计算浮动盈亏。 |
| `CloseInProfit` / `CloseInLoss` | 启用浮盈或浮亏触发。 |
| `TargetProfitInPips`, `TargetLossInPips` | 点数阈值。当标的提供 `PriceStep` 时，点数计算公式为 `价格差 / PriceStep * (成交量 / VolumeStep)`。 |
| `TargetProfitInCurrency`, `TargetLossInCurrency` | 每手货币盈亏阈值，比较前会乘以当前成交量。 |
| `TargetProfitInPercentage`, `TargetLossInPercentage` | 账户余额百分比目标。原始 EA 将浮盈直接与 `Balance ± Balance * Percentage / 100` 比较，本移植保持相同逻辑。 |

## 工作流程
1. **状态跟踪** —— 每笔成交都会更新多头与空头的持仓量以及加权平均持仓价格，因此可以正确处理对冲持仓。
2. **盈亏计算** —— 每次 Level1 行情更新都会刷新买价 / 卖价，并计算多头与空头的点数及货币盈亏。
3. **阈值判断** —— 根据目标类型和篮子模式检查对应阈值。浮盈条件使用“≥”，浮亏条件使用“≤”，与 MQL 版本保持一致。
4. **篮子平仓** —— 当条件满足时，策略会视配置撤销相关挂单，并发送市价单关闭当前持仓。

实现过程中未引入额外的集合或自定义指标，完全依赖 StockSharp 高级 API，与原始 EA 的设计理念保持一致。
