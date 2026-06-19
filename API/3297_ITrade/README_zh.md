# iTrade 策略

iTrade 策略是从 MetaTrader 专家顾问 **iTrade** 转换而来，用于手动管理做空组合。它完全复刻原始 EA 的图表按钮流程：当用户发出卖出请求时，策略会按照马丁格尔规则开仓，并持续监控所有空头的浮动盈亏，在达到指定阈值后同时平掉浮动利润最高和最低的仓位。

## 核心逻辑

- 仅在用户显式调用 `QueueSellRequest()` 时才会下达卖出市价单。
- 第一笔订单使用 **Initial Volume** 指定的手数；每出现一次亏损，下一笔订单的手数都会乘以 **Martingale Multiplier**，盈利则将序列重置为基础手数。
- 使用当前最优卖价计算浮动盈亏。当每笔持仓的平均浮盈达到 **Average Profit Target** 时，在最多 **Base Trade Count** 笔交易的范围内，平掉最赚钱和最亏损的两笔仓位。
- 当持仓数量超过 **Base Trade Count** 时，只有达到 **Extended Profit Target** 才会触发上述平仓流程。
- 盈亏计算依赖交易品种的 `PriceStep` 与 `StepPrice`，若缺少这两个属性，策略会在启动时抛出异常。

## 参数

| 名称 | 说明 |
| ---- | ---- |
| `InitialVolume` | 第一笔马丁格尔订单的基础手数。 |
| `MartingaleMultiplier` | 每次亏损后用于放大手数的倍数。 |
| `AverageProfitTarget` | 在初始批次内触发平仓的平均浮盈阈值（账户货币）。 |
| `ExtendedAverageProfitTarget` | 当持仓数超出基础批次时使用的平均浮盈阈值。 |
| `BaseTradeCount` | 视为“初始批次”的最大持仓数量。 |
| `ControlInterval` | 内部计时器的执行间隔。 |

## 使用说明

1. 启动前设置好 `Security`、`Portfolio` 以及所需参数。
2. 调用 `QueueSellRequest()` 即可模拟原 EA 的按钮，策略会自动计算手数并发送市价卖单。
3. 策略会保存最近 200 次平仓结果，以复现原始马丁格尔手数计算方式。
4. 平仓通过提交与目标仓位等量的买入市价单实现。

## 与 MetaTrader 版本的差异

- 原策略通过图表按钮触发，这里改为调用 `QueueSellRequest()`。
- 下单与成交由 StockSharp 的市场单完成，支持自动汇总部分成交。
- 盈亏阈值按照 `StepPrice` 计算货币金额，而不是使用 MetaTrader 的票据利润函数。

