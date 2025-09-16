# ComFracti分形RSI策略

## 概述
ComFracti分形RSI策略是MetaTrader专家顾问*ComFracti*的StockSharp移植版本。算法通过两个时间框架的比尔·威廉姆斯分形来判断方向性偏好，并使用基于日线的快速RSI过滤信号。一旦出现有效条件，策略将开立单一头寸，使用可配置的止损/止盈距离保护仓位，并且可选地在信号反转或持仓时间超过阈值时退出。

默认设置复刻原始EA：15分钟交易周期、1小时确认周期以及在日线开盘价上计算、周期为3的RSI。

## 交易逻辑
1. **分形方向判定**
   - 交易周期和较高周期的完结K线都会放入一个包含5根K线的滑动窗口中。
   - `Primary*Shift`与`Higher*Shift`参数决定回溯多少根已确认的分形（默认值为3，意味着检查三根K线之前才被确认的分形）。
   - 仅出现下分形（摆动低点）视为看多(+1)，仅出现上分形视为看空(-1)。
2. **日线RSI过滤**
   - 在日线周期上运行`RelativeStrengthIndex`指标，参数为`RsiPeriod`（默认3），输入值采用K线开盘价，与原始EA的`PRICE_OPEN`设置保持一致。
   - 做多需要RSI低于`50 - RsiBuyOffset`，做空需要RSI高于`50 + RsiSellOffset`。
3. **入场条件**
   - **买入**：两个时间框架的分形均给出+1，且RSI满足做多条件；若当前为空仓或空头，将买入足够数量以转为多头。
   - **卖出**：两个时间框架的分形均给出-1，且RSI满足做空条件；若当前为空仓或多头，将卖出足够数量以转为空头。
4. **仓位管理**
   - 每当仓位变动时立即根据`StopLossPips`与`TakeProfitPips`乘以合约点值计算止损与止盈价位。
   - 价格触及止损/止盈、`ExpiryMinutes`计时器到期或`CloseOnOppositeSignal`启用且信号反转时，都会执行平仓。

## 参数
| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `Volume` | 每次入场使用的下单数量。 | `0.1` |
| `TakeProfitPips` | 止盈距离（点）。为`0`时禁用止盈。 | `700` |
| `StopLossPips` | 止损距离（点）。为`0`时禁用止损。 | `2500` |
| `ExpiryMinutes` | 持仓最长时间（分钟）。`0`表示不限制。 | `5555` |
| `CloseOnOppositeSignal` | 当信号反向时是否强制平仓。 | `false` |
| `PrimaryBuyShift` | 交易周期上用于做多的分形回溯值。 | `3` |
| `HigherBuyShift` | 高周期上用于做多的分形回溯值。 | `3` |
| `PrimarySellShift` | 交易周期上用于做空的分形回溯值。 | `3` |
| `HigherSellShift` | 高周期上用于做空的分形回溯值。 | `3` |
| `RsiBuyOffset` | 做多所需的RSI低于50的偏移量。 | `3` |
| `RsiSellOffset` | 做空所需的RSI高于50的偏移量。 | `3` |
| `RsiPeriod` | 日线RSI的周期。 | `3` |
| `CandleType` | 交易周期K线类型。 | 15分钟K线 |
| `HigherTimeFrame` | 趋势确认所用的高周期K线类型。 | 1小时K线 |
| `DailyTimeFrame` | 计算RSI所用的日线K线类型。 | 1日K线 |

## 实现细节
- 采用高级K线订阅API（`SubscribeCandles().Bind(...)`），指标在策略内部维护，不会附加到`Strategy.Indicators`集合。
- 分形通过内部辅助类实现，维护滚动的5根K线并仅在分形确认后更新信号。
- RSI通过`RelativeStrengthIndex.Process(...)`以开盘价更新，完全对齐MT4版本的`PRICE_OPEN`设置。
- 策略始终只保持一个净头寸；若方向需要翻转，会自动下达足够的市价单以覆盖现有仓位。
- `GetPipSize`根据`Security.PriceStep`及`Security.Decimals`估算点值，对于三位或更多小数报价的品种采用10倍步长，模拟MT4中`Point`到点（pip）的换算。

## 使用建议
- 分形回溯值必须保证足够的历史K线可用。默认值3意味着需要至少5根完结K线才能生成信号。
- 交易不同最小报价单位的品种时（例如指数或股票），请根据实际点值调整`TakeProfitPips`和`StopLossPips`。
- 保持`CloseOnOppositeSignal`为`false`可以完全复制原EA的行为，仅依赖止损、止盈或持仓时间退出。
- 原版的逐笔加仓/风险计算依赖账户保证金信息，在StockSharp中不可用；若需要动态头寸管理，请结合外部资金管理模块或调整`Volume`参数。
