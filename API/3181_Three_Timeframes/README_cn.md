# 三重时间框架策略

## 概览
**Three Timeframes Strategy** 使用 StockSharp 高阶 API 重现 MetaTrader 专家顾问 `Three timeframes.mq5`。系统在三个时间框架上结合动量与趋势过滤条件：

- **MACD（M5）**：在交易时间框架上捕捉最新的动量反转。
- **Alligator（H4）**：在更高时间框架上确认趋势结构是否支持交易方向。
- **RSI（H1）**：在中间时间框架上验证动量是否与突破方向一致。
- 可选的 **交易时段过滤**：限制策略只在指定小时内下单。

策略使用以点（pip）为单位的风控配置。开仓后立即设置初始止损与止盈；当价格继续向有利方向运行时，可选的移动止损会在价格至少运行 `TrailingStopPips + TrailingStepPips` 后向盈利方向收紧。

## 信号逻辑
1. 策略同时订阅三个数据源：交易时间框架的K线、高时间框架K线（用于Alligator）、以及中时间框架K线（用于RSI）。
2. 做多条件：
   - MACD 主线在上一根K线下穿信号线，而再前一根K线位于信号线上方，重现原始 EA 中“蓝线向下穿越红线”的判定。
   - H1 RSI 大于 50。
   - 最近一根完成的 H4 K线中，Alligator 的 Jaw > Teeth > Lips，表示上涨结构。
3. 做空条件为镜像规则：MACD 主线向上穿越信号线，RSI 小于 50，并且 Alligator Lips > Teeth > Jaw，确认下降结构。
4. 若当前持有反向仓位，策略会先发送相应手数的市价单平掉旧仓，再按原始 EA 的逻辑开立新仓。
5. 开仓后根据参数设置初始止损/止盈，并在满足 `TrailingStopPips + TrailingStepPips` 的盈利空间后启动移动止损。

时间过滤逻辑与原版一致：当开始小时小于结束小时时，仅在该区间内允许交易；当开始小时大于结束小时时，表示跨越午夜，区间在两段时间段内合并生效。

## 风险管理
- **止损 / 止盈**：参数以点为单位，通过 `Security.PriceStep` 转换为价格增量，并针对 3 或 5 位小数报价自动调整。
- **移动止损**：只有当价格至少走出“移动止损距离 + 移动步长”后才会更新，新的止损价为多头 `当前价 - 移动距离`，空头为 `当前价 + 移动距离`。
- **交易量**：定义每次市价单的基准手数。若需要反向开仓，会先平掉旧仓再建立新仓。

## 与 MetaTrader 版本的差异
- StockSharp 的异步委托模型不再需要原始 EA 中的 `m_waiting_transaction` 标志。`BuyMarket` / `SellMarket` 会负责等待成交确认。
- MQL 中的滑点、成交方式与保证金模式设置由平台抽象管理，在 .NET 版本中无需单独处理。
- Alligator 通过三条平滑移动平均线重新构建，并用滑动缓冲区复刻 MetaTrader 指标的前移效果。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 市价单手数或合约数。 | `1` |
| `StopLossPips` | 初始止损距离（点）。 | `50` |
| `TakeProfitPips` | 初始止盈距离（点）。 | `140` |
| `TrailingStopPips` | 移动止损距离（点）。 | `5` |
| `TrailingStepPips` | 更新移动止损所需的额外点数。 | `5` |
| `MacdFastPeriod` | MACD 快速 EMA 长度。 | `13` |
| `MacdSlowPeriod` | MACD 慢速 EMA 长度。 | `26` |
| `MacdSignalPeriod` | MACD 信号平滑长度。 | `10` |
| `JawPeriod`、`TeethPeriod`、`LipsPeriod` | Alligator 三条线的 SMMA 周期。 | `13`、`8`、`5` |
| `JawShift`、`TeethShift`、`LipsShift` | Alligator 线的前移位数。 | `8`、`5`、`3` |
| `RsiPeriod` | 中间时间框架的 RSI 周期。 | `14` |
| `CandleType` | 交易时间框架（默认 5 分钟）。 | `M5` |
| `AlligatorCandleType` | Alligator 使用的高时间框架（默认 4 小时）。 | `H4` |
| `RsiCandleType` | RSI 使用的时间框架（默认 1 小时）。 | `H1` |
| `UseTimeFilter` | 是否启用时段过滤。 | `true` |
| `StartHour` | 时段起始小时（含）。 | `10` |
| `EndHour` | 时段结束小时（不含）。 | `15` |

## 使用说明
- 请确保交易品种可以提供三个所需的K线级别（默认 M5、H1、H4）。`GetWorkingSecurities()` 会自动订阅所有必要的数据流。
- 点值转换依赖于 `Security.PriceStep`。若标的的最小报价单位较特殊，需要相应调整风险参数。
- 若 `TrailingStopPips` 或 `TrailingStepPips` 设为 0，则移动止损完全禁用，这与原始 MQL 专家顾问的行为保持一致。
