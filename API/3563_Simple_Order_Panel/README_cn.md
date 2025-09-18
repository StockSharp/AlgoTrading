# Simple Order Panel 策略

该策略移植自 MetaTrader 5 工具 **SimpleOrderPanel**。StockSharp 版本保留了手动交易流程，并利用高阶策略 API 实现风险控制、头寸管理和挂单模拟。

## 主要特性

- 手动 Buy/Sell 开仓，可选择固定手数或按账户余额百分比计算仓位。
- 止损、止盈既可填写绝对价格，也可填写 MetaTrader 式的点数。
- 快捷按钮支持移动止损到保本、部分平仓以及一键全平。
- 模拟挂单：设置触发价后，价格到达时自动发送市价单（根据价格方向自动识别 limit/stop）。
- 持续监听 Level1/成交数据，在止损或止盈触发时自动平仓。

## 参数

| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `RiskCalculation` | `FixedVolume` 直接使用 `RiskValue`；`BalancePercent` 按风险百分比和止损距离计算手数。 | `FixedVolume` |
| `RiskValue` | 固定手数或风险百分比（取决于上面的模式）。 | `0.1` |
| `StopTakeCalculation` | `PriceLevels` 把数值视为价格；`PointOffsets` 乘以品种最小跳动。 | `PointOffsets` |
| `StopLossValue` | 止损数值（价格或点数），0 表示不使用。 | `300` |
| `TakeProfitValue` | 止盈数值（价格或点数），0 表示不使用。 | `300` |
| `BuyMarketRequest` | 触发多头市价单。 | `false` |
| `SellMarketRequest` | 触发空头市价单。 | `false` |
| `BreakEvenRequest` | 将止损移动到建仓价。 | `false` |
| `ModifyStopRequest` | 按当前配置重新计算止损。 | `false` |
| `ModifyTakeRequest` | 按当前配置重新计算止盈。 | `false` |
| `CloseAllRequest` | 平掉所有持仓。 | `false` |
| `CloseBuyRequest` | 仅平掉多头。 | `false` |
| `CloseSellRequest` | 仅平掉空头。 | `false` |
| `PartialCloseRequest` | 按 `PartialVolume` 手数部分平仓。 | `false` |
| `PartialVolume` | 部分平仓的手数。 | `0.05` |
| `PlaceBuyPendingRequest` | 设定买入挂单（自动选择 limit/stop）。 | `false` |
| `PlaceSellPendingRequest` | 设定卖出挂单（自动选择 limit/stop）。 | `false` |
| `PendingPrice` | 挂单触发价。 | `0` |
| `CancelPendingRequest` | 取消所有挂单。 | `false` |

布尔参数用于瞬时触发，执行完成后策略会自动重置为 `false`。

## 工作流程

1. **风险仓位**：收到指令时，根据模式选择固定手数，或者按 `RiskValue%` 风险和止损距离计算可交易手数（使用品种 `PriceStep` 与 `StepPrice` 信息）。
2. **市价开仓**：`BuyMarketRequest` 与 `SellMarketRequest` 在校验通过后发送市价单；若已有同方向仓位则忽略。
3. **挂单模拟**：保存价格、手数和触发类型，Level1 价格达到条件时转为市价单执行。
4. **保护管理**：建仓时记录入场价并换算出止损/止盈价格，后续每次报价更新都会检查是否需要平仓。
5. **仓位工具**：支持全平、仅平多头、仅平空头、部分平仓，以及保本/重置止损止盈等操作。

## 使用提示

- 需要订阅 Level1 行情，否则挂单无法触发。
- 若想获得与原面板一致的效果，请保持相同的参数数值及止损/止盈模式。
- 挂单为本地模拟，不会在交易所委托簿中显示，直到触发为止。
- 所有动作都会记录到日志，方便在 StockSharp 日志窗口中跟踪。
