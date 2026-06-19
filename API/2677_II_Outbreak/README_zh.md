# II Outbreak 策略

## 概述
II Outbreak 是一套最初在 MetaTrader 4 上实现的高频突破策略。它利用自研的时序振荡器和波动压力计捕捉方向性行情，并通过自适应跟踪止损与加仓机制管理持仓。本次移植将原始算法迁移到 StockSharp 的高层 API，同时保留对点差、波动性与日历的约束。

## StockSharp 版交易流程
### 时序振荡器
* 每根 1 分钟 K 线都会生成一条“典型价格”（High/Low/Close 的平均值乘以 100），用来驱动原策略的多层平滑链。
* 平滑链按原始 EA 的 dtemp/atemp 缓冲公式重建，输出 0 到 100 之间的时序值。
* 买入信号：当前时序值向上穿越上一值（buffer[0] > buffer[1] 且 buffer[1] ≤ buffer[2]）。
* 卖出信号：当前值向下穿越上一值（buffer[0] < buffer[1] 且 buffer[1] ≥ buffer[2]）。

### 波动性过滤
* 收盘价的 10 周期标准差必须低于 `StdDevLimit`，否则禁止开新仓并可在 `WarningAlerts` 为 true 时记录告警。
* 额外复现原策略的波动评分：利用相邻两根 K 线的重叠幅度以及“单位时间成交量”（总量 ÷ 时间），只有当得分高于 `VolatilityThreshold` 时才能交易。

### 入场条件
* 策略只针对一个证券与一个时间框架，`CandleType` 参数默认使用 1 分钟。
* 在没有持仓且通过日历过滤后，会使用 `CalculateOrderVolume()` 重新计算下单量，并将当前点差与 `SpreadThreshold` 对比（基于最优买一/卖一报价）。
* 当振荡器给出买入信号且波动条件满足时开多单；卖出信号则开空单。入场后会在距离成交价 `TrailStopPoints` × 2 的位置记录初始静态止损。

### 加仓与跟踪止损
* 当浮动利润达到 `TrailStopPoints + int(Commission) + SpreadThreshold` 点时启动跟踪模块。
* 止损价格会被更新到距离最新收盘价 `TrailStopPoints` 的位置（多空分别跟踪），只要提升超过 1 个点就会刷新。
* 在波动、时序与点差都满足的前提下，可每增长 `max(10, SpreadThreshold + 1)` 点利润加一次仓。首单加仓后静态止损失效，仅保留跟踪止损。

### 风险与资金控制
* 每次下单前都会重新计算成交量：`账户余额 × MaximumRisk ÷ (500000 / AccountLeverage)`，并根据成交量步长取整。若无法获得余额，则回退到 `Volume` 或最小手数。
* 近似复刻 MetaTrader 的保证金检查：`volume × price / leverage × (1 + MaximumRisk × 190)`。若账户价值不足，则忽略该订单。
* 当启用加仓后，策略会监控未实现亏损；若浮亏超过 `TotalEquityRisk` 所设百分比，立即平掉全部仓位。

### 日历与点差保护
* 周五 23:00 以后停止交易；年末的第 358、359、365 或 366 天在 16:00 后同样不再开仓。
* 所有初始单与加仓单都会核对当前点差，若超过阈值则放弃执行。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Commission` | 4 | 以点为单位的往返手续费，用于确定跟踪止损的启动点。 |
| `SpreadThreshold` | 6 | 允许的最大点差（点），超出则不交易也不加仓。 |
| `TrailStopPoints` | 20 | 跟踪止损距离（点），初始静态止损为该值的两倍。 |
| `TotalEquityRisk` | 0.5 | 当浮亏占账户权益的百分比达到该值时平掉所有仓位。 |
| `MaximumRisk` | 0.1 | 计算下单量时投入账户余额的比例。 |
| `StdDevLimit` | 0.002 | 10 周期标准差的上限，超过则禁止开新仓。 |
| `VolatilityThreshold` | 800 | 波动评分阈值（振幅 × 单位时间成交量）。 |
| `AccountLeverage` | 100 | 用于估算保证金与下单量的账户杠杆。 |
| `WarningAlerts` | true | 当标准差过滤器阻止开仓时是否输出告警。 |
| `CandleType` | 1 分钟 | 计算与信号使用的蜡烛类型。 |

## 指标
* `StandardDeviation(Length = 10)`：用于标准差过滤。
* 自定义的时序振荡器：直接按原 EA 公式实现，并未单独封装成 StockSharp 指标对象。

## 实现注意事项
* 点差过滤依赖 Level 1 行情数据（`Security.BestBid` / `BestAsk`）。若行情缺失则视为点差为零。
* 保证金与权益检验仅为近似值，原策略依赖 MetaTrader 的账户属性与合约面值。请根据经纪商实际情况调整 `AccountLeverage`、`MaximumRisk` 或 `Volume`。
* 代码完全使用 StockSharp 高层 API（`SubscribeCandles` + `Bind`），并按要求保留英文注释。本任务未生成 Python 版本。
