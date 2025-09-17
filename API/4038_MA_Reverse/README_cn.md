# MA Reverse 策略

## 概述
MA Reverse 策略是 MetaTrader 4 智能交易系统 “MA_Reverse” 的 StockSharp 移植版本。原始程序监控 Bid 价格在
14 周期简单移动平均线（SMA）上方或下方停留的时间，当出现足够长的连续走势时，立即进场博取价格
回归。移植后的策略同样记录每根收盘价相对于 SMA 的位置，一旦达到设定门槛就执行市价单。

## 交易逻辑
- 订阅所选周期的 K 线并按照 `SmaPeriod` 计算简单移动平均。
- 维护一个整数计数器（目标值由 `StreakThreshold` 指定），当收盘价在均线上方时递增，在均线下方时递减。
只要价格触碰均线，计数器立即归零。
- 当计数器达到 `StreakThreshold`，且收盘价至少高于 SMA `MinimumDeviation` 时，策略发送卖出市价单，押注
价格会向均线回落。
- 当计数器达到 `-StreakThreshold`，并且收盘价至少低于 SMA `MinimumDeviation` 时，策略执行买入市价单，
实现对称的做多逻辑。
- 交易完成后计数器不会重置，而是继续累计下一段走势，与 MQL 版本的实现一致。

## 订单管理
- 市价单的手数由 `TradeVolume` 控制。如果当前持有相反方向的仓位，策略会先平掉该仓位，再在同一笔
市价单中建立新的头寸，使得翻转行为与 MetaTrader 相匹配。
- 通过 StockSharp 的 `StartProtection` 功能配置全局止盈。止盈距离等于 `TakeProfitPoints` 乘以交易品种的
最小价格跳动（`PriceStep`），对应原代码中的 “30 * Point”。一旦触发目标，仓位会被市价单平仓。
- 原策略没有设置止损，这里同样保持空缺，风险控制完全依赖止盈和用户自身的资金管理。

## 参数
| 参数 | 说明 |
|------|------|
| `TradeVolume` | 每次市价进场的手数，同时用于在翻转时平掉旧仓并建立新仓。 |
| `SmaPeriod` | 简单移动平均线的周期数，默认值 14 与原策略保持一致。 |
| `StreakThreshold` | 收盘价需要连续位于 SMA 同侧的次数，达到后才允许入场。 |
| `MinimumDeviation` | 收盘价与 SMA 之间的最小绝对距离，用于确认突破是否有效。 |
| `TakeProfitPoints` | 以价格跳动数表示的止盈距离，会乘以 `PriceStep` 转换成绝对价格差。 |
| `CandleType` | 用于计算 SMA 以及评估计数器的 K 线类型（时间框架）。 |

## 备注
- 计数器基于 `SubscribeCandles` 提供的已完成 K 线工作，因此在历史回测中同样可靠。只要使用足够细的
时间框架，行为就能与基于 Tick 的 MetaTrader 版本保持一致。
- 由于 StockSharp 默认合并持仓，多笔连续入场会被视为单一仓位，并共享一个浮动止盈距离。这与
MetaTrader 为每一笔订单设置相同止盈的效果等价。
- 通过 `Bind` 绑定指标后无需手动把指标添加到 `Strategy.Indicators` 集合，生命周期由基础设施自动管理。
- 在实盘之前务必确认交易品种的 `PriceStep` 和可用手数，使 `TakeProfitPoints` 转换出的绝对止盈距离符合
您的合约规格。
