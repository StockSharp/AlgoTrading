# 平盘通道策略 (2684)

本策略是 MetaTrader 5 专家顾问 *Flat Channel (barabashkakvn 版本)* 的 C# 迁移版本。它通过 Standard Deviation 指标识别波动率持续下降的平盘区间，并在区间上下边界布置突破型止损单。当价格突破区间时，相应的止损单被触发，另一侧的挂单会立即取消，从而避免同时持有多空方向。

## 策略流程

1. **波动率过滤**：订阅蜡烛数据并计算中位价的标准差，若标准差连续 `FlatBars` 根以上下降，则认定进入平盘阶段。
2. **构建价格通道**：记录平盘阶段内的最高价和最低价。通道宽度需要保持在 `ChannelMinPips` 与 `ChannelMaxPips` 之间（会通过 `PriceStep` 自动换算成价格单位）。
3. **挂单入场**：当价格仍位于通道内部时，策略会：
   - 在通道上轨放置 Buy Stop，止损设置在入场价下方 `2 × 通道宽度`，止盈设置在入场价上方 `1 × 通道宽度`；
   - 在通道下轨放置 Sell Stop，对称设置止损与止盈。
4. **挂单有效期**：`OrderLifetimeSeconds` 决定挂单的最长期限，到期后未成交的止损单会被撤销，如果平盘条件仍成立则可以重新生成。
5. **持仓管理**：当挂单成交后，另一侧挂单被取消，同时为持仓重新登记止损与止盈订单。若 `UseBreakeven` 启用，当价格朝着目标运行到 `FiboTrail` 指定的 Fibonacci 比例时，止损会移动到开仓价以锁定无损状态。
6. **时间过滤**：`UseTradingHours` 参数可按星期以及周一启动时间、周五停止时间限制交易窗口，完全复刻原版 EA 的时间控制逻辑。

## 指标

- **StandardDeviation**（中位价，周期 `StdDevPeriod`）用于判断波动率是否持续下降。
- **DonchianChannels**（周期 `FlatBars`）提供初始的通道上下轨。

## 风险控制与仓位

- 关闭资金管理时，`FixedVolume` 为每次下单的固定手数。
- 打开 `UseMoneyManagement` 后，会按照 `RiskPercent` 的资金风险和止损距离（结合 `PriceStep` 与 `StepPrice`）来估算下单数量。
- 如果上一笔交易亏损，下一次下单会使用 `FixedVolume × 4` 的手数，对应原程序的追赶机制。

## 参数说明

| 参数 | 含义 |
|------|------|
| `UseTradingHours` | 是否启用交易时间过滤。 |
| `TradeTuesday`, `TradeWednesday`, `TradeThursday` | 控制周二、周三、周四是否允许交易。 |
| `MondayStartHour`, `FridayStopHour` | 周一开始交易的小时以及周五停止交易的小时（0–23）。 |
| `UseMoneyManagement`, `RiskPercent`, `FixedVolume` | 仓位管理相关设置。 |
| `OrderLifetimeSeconds` | 挂单有效期（秒），0 表示永不过期。 |
| `StdDevPeriod`, `FlatBars` | 指标周期与平盘确认的最少根数。 |
| `ChannelMinPips`, `ChannelMaxPips` | 通道最小/最大宽度，单位为点。 |
| `UseBreakeven`, `FiboTrail` | 是否启用止损保本以及触发保本的 Fibonacci 倍数。 |
| `CandleType` | 计算所用的蜡烛类型或时间框架。 |

## 其他提示

- 需要标的提供 `PriceStep` 和 `StepPrice` 信息才能把点值换算为真实价格。
- 一旦标准差不再下降，平盘状态会被重置并撤销所有挂单。
- 头寸平仓后会自动取消对应的止损和止盈订单，避免残留挂单。

## 免责声明

本文档仅供学习与参考，不构成任何投资建议。请务必在模拟或历史数据上充分验证策略，并根据自身风险承受能力调整参数后再进行实盘应用。
