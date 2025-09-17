# Master Exit Plan 策略

## 概述

`MasterExitPlanStrategy` 将 MetaTrader 顾问 "Master Exit Plan" 的风控逻辑移植到 StockSharp。该策略不会主动开仓，而是监控已有头寸：根据多组止损规则管理风险，跟踪挂单，并在权益达到目标百分比时一次性平仓。

策略订阅 1 分钟 K 线，以模拟原脚本中的 `iOpen(symbol, PERIOD_M1, 1)` 调用。计时器每秒触发一次，对应 MQL4 的 `EventSetTimer(1)` 行为。

## 功能

- **权益目标退出**：当组合权益增长达到设定百分比时立即清仓。
- **静态与动态止损**：同时监控开仓价距离与上一分钟开盘价派生的动态阈值。
- **隐藏止损**：不在交易所挂单，而是由策略内部发送市价单完成保护。
- **追踪止损模块**：在收益达到最小阈值后启用，并自动考虑当前点差。
- **挂单追踪**：自动重新登记 buy stop / sell stop 订单，使其始终贴近行情。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `EnableTargetEquity` | 是否启用权益目标退出。 | `false` |
| `TargetEquityPercent` | 权益目标的百分比幅度。 | `1` |
| `EnableStopLoss` | 启用静态（交易所式）止损。 | `false` |
| `StopLossPoints` | 静态止损距离（点）。 | `2000` |
| `EnableDynamicStopLoss` | 将止损锚定到上一分钟开盘价。 | `false` |
| `DynamicStopLossPoints` | 动态止损距离（点）。 | `2000` |
| `EnableHiddenStopLoss` | 启用隐藏静态止损。 | `false` |
| `HiddenStopLossPoints` | 隐藏静态止损距离（点）。 | `800` |
| `EnableHiddenDynamicStopLoss` | 启用隐藏动态止损。 | `false` |
| `HiddenDynamicStopLossPoints` | 隐藏动态止损距离（点）。 | `800` |
| `EnableTrailingStop` | 启用追踪止损模块。 | `false` |
| `TrailingStopPoints` | 追踪止损距离（点）。 | `5` |
| `TrailingTargetPercent` | 启用追踪前所需的权益收益百分比。 | `0.2` |
| `SureProfitPoints` | 启用追踪前需要额外锁定的点数。 | `30` |
| `EnableTrailPendingOrders` | 是否追踪未成交的 stop 挂单。 | `false` |
| `TrailPendingOrderPoints` | 挂单与行情之间的目标距离（点）。 | `10` |

## 使用提示

1. 将策略绑定到由其他模块或人工开仓的证券上。`Volume` 应设置为在清仓时需要反向成交的合约数量。
2. 投资组合需提供 `Portfolio.CurrentValue`，用于模拟 MetaTrader 的 `AccountBalance` / `AccountEquity`。若该值缺失，权益目标功能不会触发。
3. 策略使用最优买卖价计算点差，请确保有 Level1 行情数据。
4. 所有保护动作均通过市价单完成，未在交易所生成真实止损单，保持与原顾问“隐藏止损”的逻辑一致。

## 与 MQL 版本的差异

- 不再调用 `OrderModify`，而是通过定时检查并在触发阈值时直接平仓。
- 动态止损依赖 `SubscribeCandles` 提供的最新一分钟收盘蜡烛；没有蜡烛数据时相关逻辑自动停用。
- 挂单追踪仅处理当前策略创建的 stop 挂单，其他模块的保护单不会被移动。
- 权益计算基于 `Portfolio.CurrentValue`（若为空则回退到 `Portfolio.BeginValue`）。

## 测试

策略未附带自动化测试。请先在 StockSharp 模拟器中使用历史数据回测，再部署到真实环境。
