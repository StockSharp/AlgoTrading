# HTH篮子策略

此目录包含 MetaTrader 专家顾问 **HTH.mq4**（Hedge That Hedge）的 StockSharp 高层 API 版本。原始 EA 会在交易日开始后的几分钟内同时开出四个主要外汇货币对头寸，并以点数(Pip)为单位进行风控管理。本移植版本通过烛线订阅、`StrategyParam` 参数系统以及按品种存储的状态，实现与原脚本一致的决策流程。

## 交易思路

1. 需要为策略指定四个货币对。第一个货币对绑定到策略的 `Security` 属性，其余三个通过 `Symbol2`、`Symbol3` 与 `Symbol4` 参数设置。
2. 每个品种订阅两种烛线：日线提供最近两天的收盘价，分钟级（默认 1 分钟）烛线提供当前价格并用于判定是否处于午夜时间窗口。
3. 当时间来到 00:05 至 00:12（含两端）之间，如果当天尚未建立篮子，则读取主品种上一交易日的偏离值（当日收盘相对前一日收盘的百分比变化）。
4. 偏离值为正时开多第一、第二、第四个货币对并做空第三个货币对；偏离值为负则方向全部取反。四条腿使用相同的 `TradeVolume`。
5. 白天运行期间持续计算四条腿的点数合计。当 `ShowProfitInfo` 为真时会将该数值写入日志，并驱动风控动作。
6. 到了 23:00（平台时间）会无条件平掉所有腿，与 MQL 脚本保持一致。

## 篮子管理

- **应急加仓**：若 `AllowEmergencyTrading` 为真，则在每次开仓前重置应急标志。当篮子回撤达到 `-EmergencyLossPips` 且存在盈利腿时，会按原方向、原手数加仓该腿，仅触发一次，对应 MQL 中的 `doubleorders()`。
- **盈利目标**：启用 `UseProfitTarget` 且点数合计达到 `ProfitTargetPips` 时，全部腿都会被平掉。
- **亏损限制**：启用 `UseLossLimit` 且点数合计低于 `-LossLimitPips` 时，篮子立即止损。
- 以上风控动作仅在 `ShowProfitInfo` 打开时执行，完全复现原脚本中由 `show_profit` 控制的逻辑。

主品种每分钟都会输出一条“Deviation snapshot”日志，内容包含：

- 四个货币对的即时偏离以及上一交易日偏离值；
- 六种货币对组合的偏离（加减组合），格式与原脚本 `Comment()` 相同；
- 当前篮子的点数收益。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TradeEnabled` | true | 是否允许在每日窗口开仓。|
| `ShowProfitInfo` | true | 记录点数收益并激活风险管理。|
| `UseProfitTarget` | false | 点数≥`ProfitTargetPips` 时全部平仓。|
| `UseLossLimit` | false | 点数≤`-LossLimitPips` 时全部平仓。|
| `AllowEmergencyTrading` | true | 启用应急加仓功能。|
| `EmergencyLossPips` | 60 | 触发应急加仓的回撤点数阈值。|
| `ProfitTargetPips` | 80 | 达到该点数后停止交易并平仓。|
| `LossLimitPips` | 40 | 亏损达到该点数后立即平仓。|
| `TradeVolume` | 0.01 | 四条腿开仓的手数。|
| `Symbol2` | — | 第二个货币对（原脚本默认为 USDCHF）。|
| `Symbol3` | — | 第三个货币对（原脚本默认为 GBPUSD）。|
| `Symbol4` | — | 第四个货币对（原脚本默认为 AUDUSD）。|
| `IntradayCandleType` | 1 分钟 | 用于窗口判定和价格更新的烛线周期。|

## 实现说明

- 每个品种会订阅两个烛线序列：分钟线负责时间窗口与价格更新，日线负责偏离计算；所有订阅均通过 `Bind` 完成。
- `InstrumentState` 保存了每个品种的最新价格、近两日收盘价、仓位均价与持仓量，从而无需访问指标历史即可计算点数收益。
- 点数收益使用品种的 `PriceStep`（若为空则退回 `MinPriceStep`）换算，效果等同于 MQL 中使用 `MODE_POINT`。
- 策略仅处理 **收盘完成** 的烛线，符合移植规范。
- 每次建立新篮子都会重置应急标志；一旦触发加仓会将其关闭，直到下一交易日重新开启，行为与原脚本的 `enable_emergency_trading` 完全一致。
