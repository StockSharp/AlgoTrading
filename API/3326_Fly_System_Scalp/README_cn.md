# Fly System Scalp 策略

## 概述
Fly System Scalp 策略源自 MQL4 专家顾问 *FlySystemEA*。策略持续监听盘口最佳买价和卖价，在市场两侧放置对称的止损挂单（Buy Stop / Sell Stop），以捕捉短期突破机会。同时严格控制点差、佣金和可交易时段，确保系统适用于高频剥头皮场景。

## 交易逻辑
1. **行情输入**：订阅标的的 Level-1 行情，随时更新最近的买/卖报价。
2. **前置检查**：每个行情更新都会验证：
   * 策略在线且允许交易；
   * 当前时间位于可交易时段内（可选）；
   * 当前点差加上 `CommissionInPips` 不超过 `MaxSpread`。
3. **挂单布置**：当检查全部通过且账户无持仓时，策略会创建两单：
   * Buy Stop：`Ask + PendingDistance * pip`，同时附加止损与可选的止盈；
   * Sell Stop：`Bid - PendingDistance * pip`，止损/止盈距离与买单镜像。
   若市场价格偏离已提交挂单超过 `ModifyThreshold`（以点为单位），系统会重新提交挂单。
4. **挂单维护**：当任意挂单触发并形成持仓时，另一侧挂单立即取消。若点差或时间过滤条件失效，系统撤销所有挂单并等待条件恢复。
5. **仓位管理**：若启用 `AutoLotSize`，成交量按照账户权益的 `RiskFactor`% 与止损距离计算；否则使用固定的 `ManualVolume`。
6. **安全保护**：调用 `StartProtection()`，利用 StockSharp 的内置防护功能处理异常情况。

## 参数
| 名称 | 说明 | 默认值 |
|------|------|--------|
| `PendingDistance` | 挂单距离市场价格的点数。 | 4 |
| `StopLossDistance` | 止损距离（点）。 | 0.4 |
| `TakeProfitDistance` | 止盈距离（点）。 | 10 |
| `UseTakeProfit` | 是否启用止盈。 | `false` |
| `MaxSpread` | 允许的最大点差（点）；0 表示不限制。 | 1 |
| `CommissionInPips` | 计入点差过滤的佣金（点）。 | 0 |
| `AutoLotSize` | 是否启用自动仓位计算。 | `false` |
| `RiskFactor` | 自动仓位计算的风险比例（%）。 | 10 |
| `ManualVolume` | 关闭自动仓位时使用的固定手数。 | 0.1 |
| `UseTimeFilter` | 是否启用交易时段过滤。 | `false` |
| `TradeStartTime` | 交易时段起始时间（包含）。 | 00:00:00 |
| `TradeStopTime` | 交易时段结束时间（不包含）。 | 00:00:00 |
| `ModifyThreshold` | 重新提交挂单前允许的价格偏差（点）。 | 1 |

## 使用说明
* 自动仓位计算依赖证券的 `Step`、`PriceStep`、`StepPrice`、`LotStep`、`MinVolume`、`MaxVolume` 等属性；若缺失会回退到固定手数。
* Pip 大小依据价格步长与小数位估算，兼容 3 位或 5 位小数的外汇品种。
* 当 `UseTimeFilter=true` 且 `TradeStartTime=TradeStopTime` 时视为全天可交易；若起始时间大于结束时间则表示跨越午夜的交易区间。
* 点差过滤会把 `CommissionInPips` 加到实时点差后再比较，重现原始 EA 的逻辑。
* 策略不创建任何图表对象，可在前端自行绑定行情进行可视化。

## 与原版 EA 的差异
* 移除了 MQL 中的计时器与界面对象，完全依赖 StockSharp 的事件模型与日志。
* 挂单调整逻辑简化为“偏差超过阈值则重新提交”，避免原策略中复杂的多分支条件。
* 自动佣金识别被简化为手动参数 `CommissionInPips`，但仍将其纳入点差风控。
* 使用 `StartProtection()` 替代自定义的冻结/止损级别监控。

## 回测建议
需要提供包含买/卖价的 Level-1 历史数据或由逐笔成交构造的双向报价序列，以便准确模拟挂单触发。仅有 K 线数据不足以再现策略行为。
