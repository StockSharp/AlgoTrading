# 随机震荡加速策略

## 概述
随机震荡加速策略来源于 MetaTrader 5 专家顾问 *#2 stoch mt5*。原始机器人将三个随机震荡指标与 Bill Williams 的
Accelerator Oscillator 以及 Awesome Oscillator 组合使用。只有当所有随机震荡过滤器都确认多头动能，并且 Accelerator
Oscillator 向上突破设定阈值时才会开多。空头信号使用相同的镜像规则。持仓期间 Awesome Oscillator 负责监控动能，一旦
穿越预设带状区域就关闭仓位。StockSharp 版本利用高级别的 K 线订阅 API 和指标绑定来复现这些机制。

策略保留了原专家的资金管理配置。开仓量固定为指定手数，止损和止盈距离以 MetaTrader 点（pip）表示。实现中调用
`StartProtection`，因此每笔新交易都会自动附加相同的保护距离。为了保持与 MetaTrader 一致的风险控制，策略会根据
交易品种的价格最小变动单位计算 pip 大小。

## 交易逻辑
1. 订阅由 `CandleType` 指定的主时间框架，只在 K 线收盘后处理数据，与原 EA 的做法一致。
2. 驱动三个 `StochasticOscillator` 指标：
   - **信号随机指标** 判断 %K 是否位于 %D 上方或下方。
   - **入场随机指标** 要求做多信号高于 `EntryLevel`（做空信号低于 `100 - EntryLevel`）。
   - **过滤随机指标** 限制做多信号必须低于 `FilterLevel`（做空信号高于 `100 - FilterLevel`）。
3. 跟踪 Accelerator Oscillator，并要求其向上穿越 `AcceleratorLevel` 才允许买入；做空时需要向下穿越 `-AcceleratorLevel`。
4. 当 Awesome Oscillator 向相反方向穿越 `AwesomeLevel` 区域时立即平仓。
5. 平仓后，如果只有一个方向满足全部条件，则按该方向开仓。下单量会根据品种的 `VolumeStep` 自动调整，以便满足实盘
   交易所需的最小变动手数。
6. 使用 `StartProtection` 设置止损与止盈距离，继承 MetaTrader 中基于 pip 的风险管理。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 4 小时周期 | 策略处理的主 K 线序列。 |
| `TradeVolume` | `decimal` | `0.01` | 每次开仓的交易量（手数）。 |
| `StopLossPips` | `decimal` | `40` | 止损距离（MetaTrader pip）。 |
| `TakeProfitPips` | `decimal` | `70` | 止盈距离（MetaTrader pip）。 |
| `SignalKPeriod` | `int` | `40` | 信号随机指标的 %K 周期。 |
| `SignalDPeriod` | `int` | `10` | 信号随机指标的 %D 平滑周期。 |
| `SignalSlowing` | `int` | `10` | 信号随机指标的额外平滑参数。 |
| `EntryKPeriod` | `int` | `40` | 入场随机指标的 %K 周期。 |
| `EntryDPeriod` | `int` | `10` | 入场随机指标的 %D 平滑周期。 |
| `EntrySlowing` | `int` | `10` | 入场随机指标的额外平滑参数。 |
| `EntryLevel` | `decimal` | `20` | 多头确认阈值，空头使用 `100 - EntryLevel`。 |
| `FilterKPeriod` | `int` | `40` | 过滤随机指标的 %K 周期。 |
| `FilterDPeriod` | `int` | `10` | 过滤随机指标的 %D 平滑周期。 |
| `FilterSlowing` | `int` | `10` | 过滤随机指标的额外平滑参数。 |
| `FilterLevel` | `decimal` | `75` | 多头上限阈值，空头使用 `100 - FilterLevel`。 |
| `AcceleratorLevel` | `decimal` | `0.0002` | Accelerator Oscillator 的最小入场幅度。 |
| `AwesomeLevel` | `decimal` | `0.0013` | Awesome Oscillator 触发离场的带状阈值。 |

## 与原 MetaTrader 专家的差异
- StockSharp 版本通过 K 线订阅和指标绑定获取数据，不再重复调用 `CopyBuffer`。
- 策略在净持仓模式下运作：需要反向时先平掉当前仓位，再提交反向的市价单。
- 通过 `StartProtection` 自动附加止损和止盈，距离依据合约的最小报价步长换算成 pip，避免人工修改委托。
- 下单量会根据 `VolumeStep`、`MinVolume` 与 `MaxVolume` 自动归一化，便于直接接入真实交易环境。

## 使用建议
- 启动前请将 `TradeVolume` 调整到交易品种的最小手数步长。
- 根据市场特性同步优化随机指标的阈值（`EntryLevel`、`FilterLevel`）以及两个振荡器的敏感度。
- 在支持图表的环境中运行可同时观察三个随机指标、Accelerator Oscillator、Awesome Oscillator 以及成交记录。
- 策略只在 K 线收盘后发出信号，建议在相同时间框架下回测或联机运行，以获得一致的行为。

## 指标
- 三个参数独立的 `StochasticOscillator`。
- `AcceleratorOscillator` 用于确认入场。
- `AwesomeOscillator` 用于判断离场时机。
