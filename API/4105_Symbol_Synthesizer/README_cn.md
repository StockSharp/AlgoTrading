# 符号合成器策略

## 概述

**Symbol Synthesizer Strategy** 是 MetaTrader 5 专家顾问 *SymbolSynthesizer.mq5* 的 C# 版本。原始脚本通过一个手动面板展示 13 组货币组合，实时计算由两条腿构成的合成汇率，并在操作者点击按钮时发送两笔对冲订单。本移植版本保留了这些核心特性：

* 订阅两条腿的 Level1 数据，持续计算合成的买价和卖价。
* 提供与 MQL5 `Sym` 数组完全一致的 13 组货币组合。
* 当操作者请求合成 **买入** 或 **卖出** 时，同时发送两笔对冲委托。
* 使用原始公式按照 Tick Value 和 Point 的比例调整第二条腿的手数，以保持合成头寸的名义敞口。

策略保持手动执行的设计。当 `TradeAction` 参数被设置为 `Buy` 或 `Sell` 时，它才会根据最新报价触发下单操作，等同于 MetaTrader 面板上的按钮。

## 预设组合

下表复刻了 MQL5 中的 `Sym` 数组。索引 `0` 与原脚本的图表符号一致，也是 `CombinationIndex` 参数的默认值。

| 索引 | 合成品种 | 第一条腿 | 第二条腿 | 组合方式 |
|------|----------|----------|----------|----------|
| 0 | EURUSD | EURGBP | GBPUSD | 乘积（两条腿方向一致） |
| 1 | GBPUSD | EURGBP | EURUSD | 比值（第一条腿方向与第二条腿相反） |
| 2 | USDCHF | EURUSD | EURCHF | 比值 |
| 3 | USDJPY | EURUSD | EURJPY | 比值 |
| 4 | USDCAD | EURUSD | EURCAD | 比值 |
| 5 | AUDUSD | EURAUD | EURUSD | 比值 |
| 6 | EURGBP | GBPUSD | EURUSD | 比值 |
| 7 | EURAUD | AUDUSD | EURUSD | 比值 |
| 8 | EURCHF | EURUSD | USDCHF | 乘积 |
| 9 | EURJPY | EURUSD | USDJPY | 乘积 |
| 10 | GBPJPY | GBPUSD | USDJPY | 乘积 |
| 11 | AUDJPY | AUDUSD | USDJPY | 乘积 |
| 12 | GBPCHF | GBPUSD | USDCHF | 乘积 |

“乘积”表示两条腿的下单方向相同；“比值”表示第一条腿的方向与第二条腿相反。

## 参数说明

| 名称 | 说明 |
|------|------|
| `CombinationIndex` | 预设组合的索引（0–12）。组合在启动时确定，修改后需重启策略。 |
| `OrderVolume` | 第一条腿的初始手数。策略会根据腿的手数步长进行归一化，并确保满足最小手数要求。 |
| `Slippage` | 允许的最大滑点，单位为价格步长。限价单会在参考买价/卖价的基础上偏移 `Slippage × PriceStep`，模拟 MetaTrader 的允许偏差。 |
| `TradeAction` | 手动触发值（`None`、`Buy`、`Sell`）。设置为 `Buy` 或 `Sell` 即可模拟面板按钮；执行成功或记录错误后会自动恢复为 `None`。 |

## 数据订阅

策略会对两条腿订阅 Level1（最优买卖）数据，当报价充分时按以下公式更新合成价格：

* 乘积：`vBid = bid1 × bid2`，`vAsk = ask1 × ask2`
* 比值：`vBid = bid2 / bid1`，`vAsk = ask2 / ask1`

每次合成报价变化都会写入日志，方便操作者跟踪虚拟汇率。

## 下单逻辑

1. 第一条腿的手数等于归一化后的 `OrderVolume`。
2. 第二条腿的手数沿用 MQL5 公式：
   
   `vol2 = vol1 × syntheticPrice ÷ tickValue1 ÷ tickValue2 × (point2 ÷ point1)`
   
   其中 `tickValue` 对应 `Security.StepPrice`，`point` 对应 `Security.PriceStep`。
3. 方向规则与原脚本一致：
   * **乘积组合：** 第一条腿与第二条腿都沿着请求的方向下单。
   * **比值组合：** 第一条腿与请求方向相反，第二条腿与请求方向相同。
4. 下单价格来自每条腿的最新买价或卖价，限价单会加入滑点偏移，并使用 `Security.ShrinkPrice` 进行价格归一化。

如果缺少必须的合约元数据（价格步长、Tick Value、手数步长），策略会记录错误并放弃该次下单，以保持与原专家顾问一致的防护行为。

## 使用提示

* 启动前请设置主 `Security` 与 `Portfolio`。策略会通过 StockSharp 的符号查询自动解析额外的腿品种。
* 数据源需要提供正确的 `PriceStep`、`StepPrice` 与 `VolumeStep`，否则无法计算对冲手数。
* 策略仅提供手动触发功能，没有添加额外的自动化交易逻辑。
* 如需切换至其他组合索引，请停止并重新启动策略。
