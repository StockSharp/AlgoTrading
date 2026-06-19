# Andrew's Pitchfork 策略

这是 MetaTrader 智能交易系统 “Andrew's Pitchfork” 的移植版本。原脚本需要交易者手工绘制安德鲁斯分叉线，再配合动量、高周期加权均线和 MACD 滤波。StockSharp 版本保留了指标组合，利用自动趋势判定代替手工分叉方向检查，同时还原了风险管理模块（限制加仓次数、止损止盈、保本和跟踪止损）。

## 交易逻辑

1. **指标**
   - 两条基于典型价格的 *线性加权移动平均线*（LWMA）。
   - 同一时间框架上的 *Momentum* 振荡指标，使用与 100 的绝对偏差来评估强度。
   - 经典的 *MACD (12, 26, 9)* 与信号线组合。
2. **开仓条件**
   - **做多**：快速 LWMA 位于慢速 LWMA 上方，最近三次动量偏差至少有一次超过 `MomentumBuyThreshold`，且 MACD 线高于信号线。
   - **做空**：条件完全相反。
   - 策略允许在绝对仓位低于 `Volume * MaxPyramids` 时继续按基础 `Volume` 加仓。反向信号会先平掉现有仓位再开立新方向。
3. **风控模块**
   - 初始止损/止盈以价格步长为单位放置在入场价附近，仓位变化时会重新计算。
   - 保本逻辑在盈利达到指定步数后移动止损。
   - 跟踪止损根据新的价格极值和额外缓冲不断调整。

与 MQL 版本相比，StockSharp 移植通过 LWMA 的斜率自动判断趋势，不再依赖手工绘制分叉对象。其他过滤器（动量、MACD、加仓限制）以及资金管理规则均通过 StockSharp 高级 API 实现。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CandleType` | `DataType` | 15 分钟 | 所有指标使用的主蜡烛序列。 |
| `FastMaPeriod` | `int` | 6 | 快速 LWMA 的周期。 |
| `SlowMaPeriod` | `int` | 85 | 慢速 LWMA 的周期。 |
| `MomentumPeriod` | `int` | 14 | Momentum 指标的回溯长度。 |
| `MomentumBuyThreshold` | `decimal` | 0.3 | 做多所需的 \|Momentum − 100\| 最小值。 |
| `MomentumSellThreshold` | `decimal` | 0.3 | 做空所需的 \|Momentum − 100\| 最小值。 |
| `MaxPyramids` | `int` | 1 | 同方向最多可以持有的基础手数。 |
| `StopLossSteps` | `int` | 20 | 以价格步长表示的止损距离。 |
| `TakeProfitSteps` | `int` | 50 | 以价格步长表示的止盈距离。 |
| `EnableTrailing` | `bool` | `true` | 是否启用跟踪止损。 |
| `TrailingTriggerSteps` | `int` | 40 | 激活跟踪止损所需的盈利步数。 |
| `TrailingDistanceSteps` | `int` | 40 | 价格极值与跟踪止损之间保持的步数间隔。 |
| `TrailingPadSteps` | `int` | 10 | 跟踪止损的额外缓冲步数。 |
| `EnableBreakEven` | `bool` | `true` | 是否启用保本。 |
| `BreakEvenTriggerSteps` | `int` | 30 | 触发保本所需的盈利步数。 |
| `BreakEvenOffsetSteps` | `int` | 30 | 保本时止损相对于入场价的偏移步数。 |

## 说明

- 策略需要合约提供有效的 `PriceStep`，用于将步长转换为实际价格；如果缺失，保本和跟踪逻辑不会执行。
- 每当仓位变化时，止损和止盈订单都会重新创建，以匹配新的持仓规模。
- 默认参数复刻了原始 EA 的设置，可通过 `StrategyParam` 提供的范围进行优化。
