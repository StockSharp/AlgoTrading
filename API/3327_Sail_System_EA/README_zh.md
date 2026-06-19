# Sail System EA 策略

## 概述
Sail System EA 是一款对冲型剥头皮策略，会同时保持多头和空头敞口，并实时监控
经纪商的要求（最大点差、最小止损距离以及交易时段限制）。StockSharp 版本完全使
用高级 `Strategy` API：策略订阅一级报价，自动重新布置双向头寸，并在策略内部管理
虚拟止损/止盈，无需调用底层连接器接口。

实现中维护了两个 `PositionState` 实例（多头与空头）。每个对象都保存进场价格、剩余
持仓量、虚拟保护位以及挂单状态，与 MQL 原版中为市价单和挂单分别分配票据的做法对
应。

## 交易逻辑
1. **时间过滤。** 可以将交易限制在指定的时间窗口内。一旦当前时间超出窗口，策略会
   根据 `ManageExistingOrders` 的设置选择保留头寸、撤销挂单或直接市价平仓。
2. **点差监控。** 通过 `SubscribeLevel1()` 获取买一/卖一。可选择检测即时点差，或保
   留不超过 100 个样本的滑动平均值，再与 `MaxSpread` 加上佣金的阈值比较。若点差过
   宽，可选地平掉持仓，并用 `MultiplierIncrease` 放大入场距离，等待市场恢复。
3. **入场机制.** 在允许交易的时段内，根据 `UsePendingOrders` 决定直接以市价同时开多
   和开空，还是维护一对限价挂单。限价价格取当前最优 bid/ask 加上 `DistancePending`
   （以点为单位），并可乘以安全系数。
4. **虚拟保护。** 每次成交都会依据 `OrdersStopLoss` / `OrdersTakeProfit` 设定虚拟止损与
   可选的止盈。每经过 `DelayModifyOrders` 次报价更新，若收益改善超过
   `StepModifyOrders`，就重新计算保护位，从而复刻 MQL 中逐步上移止损的行为。
5. **离场处理。** 当多头的 bid 或空头的 ask 到达虚拟止损/止盈时，立即发出反向市价单
   平仓，并在日志中记录触发原因（止损、止盈、时段结束或点差超限）。
6. **重新布单。** 如果挂单偏离市场超过 `PipsReplaceOrders` * `SafeMultiplier`，则撤单并
   按最新价格重新挂出，替代了原脚本使用计时器反复调整的方案。
7. **仓位大小。** 可以直接使用固定的 `ManualLotSize`，也可以按照账户权益和
   `RiskFactor` 自动计算头寸规模，与 MQL 的自动手数逻辑相同。

## 参数
| 参数 | 说明 |
|------|------|
| `OrderVolume` / `ManualLotSize` | 禁用自动手数时的基础下单量。 |
| `AutoLotSize`, `RiskFactor` | 启用按权益计算手数。 |
| `UseVirtualLevels` | 是否在策略端维护虚拟止损/止盈。 |
| `OrdersStopLoss`, `OrdersTakeProfit`, `PutTakeProfit` | 保护距离（点）。 |
| `DelayModifyOrders`, `StepModifyOrders` | 控制虚拟保护位的刷新频率与最小移动幅度。 |
| `PipsReplaceOrders`, `SafeMultiplier` | 当挂单偏离过远时重新布单。 |
| `UsePendingOrders`, `DistancePending` | 切换限价挂单或即时市价入场。 |
| `UseTimeFilter`, `TimeStartTrade`, `TimeStopTrade`, `ManageExistingOrders` | 交易时段配置。 |
| `MaxSpread`, `TypeOfSpreadUse`, `HighSpreadAction`, `MultiplierIncrease`, `CloseOnHighSpread` | 点差过滤与处理策略。 |
| `CommissionInPip`, `CountAvgSpread`, `TimesForAverage` | 点差平均与佣金处理。 |
| `AcceptStopLevel`, `Slippage`, `OrdersId` | 经纪商最小止损、滑点限制以及 magic number 等效值。 |

所有参数都通过 `StrategyParam<T>` 暴露，可在 Designer 中编辑，也支持优化。

## 与 MQL 版本的差异
- StockSharp 采用净头寸模型，因此当一侧成交后会取消另一侧挂单，避免净头寸归零，
  但仍保持原有“配对对冲”节奏。
- `UseVirtualLevels` 让止损/止盈逻辑保留在策略内。MQL 原版在图表上绘制对象，此处以
  日志记录替代。
- 点差平均值使用增量方式计算，无需数组，同时保留了相同的样本数量限制。

## 高级 API 使用
- 通过 `SubscribeLevel1().Bind(ProcessLevel1)` 接收最优买卖价并驱动全部决策。
- 入场与离场统一通过 `RegisterOrder` 以及 `BuyMarket`/`SellMarket` 封装的高阶方法完成。
- 在 `OnStarted` 中调用 `StartProtection()` 一次，按照框架最佳实践启用保护性订单支持。
