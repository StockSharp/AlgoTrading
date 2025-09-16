# Extreme EA（StockSharp 版本）

**Extreme EA** 是一套基于趋势的自动交易系统，原始实现来自 MetaTrader。策略使用两条移动平均线与 CCI（顺势指标）组合，并附带自适应的资金管理模块。本移植版完全依托 StockSharp 高阶 API，在保持原始逻辑的同时，使所有关键参数都能在界面中调节。策略仅处理已完成的 K 线，并通过独立的订阅支持多周期数据输入。

## 策略结构

1. **趋势过滤**：在参数 `MaCandleType` 指定的周期上计算快、慢两条移动平均线。快线负责捕捉短期动量，慢线刻画主要趋势。为了复现 MQL 中 `CopyBuffer` 的索引，本策略缓存慢线最近两根柱子的数值以判断斜率。
2. **动量过滤**：CCI 在独立的 `CciCandleType` 周期以及自选价格类型上计算。每次 CCI 周期结束时更新缓存，期间复用该值，以匹配 MetaTrader 指标缓冲区的行为。
3. **开仓条件**：
   - 当慢线向上、快线向上且 CCI 低于下轨时买入。
   - 当慢线向下、快线向下且 CCI 高于上轨时卖出。
4. **平仓条件**：
   - 慢线不再上升时平掉全部多单。
   - 慢线不再下降时平掉全部空单。

## 风险管理

- **MaximumRisk** 根据账户权益与最新价格估算下单量。当权益或报价不可用时，自动退回到 `Volume` 或交易所允许的最小手数。
- **DecreaseFactor** 在连续出现两笔及以上亏损时按 `volume - volume * losses / DecreaseFactor` 公式缩减手数，复刻原版 EA 的“递减手数”机制。
- **HistoryDays** 用于限制亏损串的记忆时长。若平仓时间距离上一笔记录超过该天数，递减计数会被重置。
- **MaxPositions** 控制同方向的最大累计仓位，防止无限制加仓。超过上限时新的入场信号会被忽略。

## 参数列表

| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `MaximumRisk` | `decimal` | `0.05` | 单笔交易占用的权益比例。 |
| `DecreaseFactor` | `decimal` | `6` | 连续亏损时的手数缩减系数，`0` 表示关闭。 |
| `HistoryDays` | `int` | `60` | 亏损串记忆的天数上限。 |
| `MaxPositions` | `int` | `3` | 同方向最多允许的持仓次数。 |
| `FastMaPeriod` | `int` | `15` | 快速均线周期。 |
| `SlowMaPeriod` | `int` | `75` | 慢速均线周期。 |
| `CciPeriod` | `int` | `12` | CCI 计算窗口。 |
| `CciUpperLevel` | `decimal` | `50` | 触发做空的 CCI 上阈值。 |
| `CciLowerLevel` | `decimal` | `-50` | 触发做多的 CCI 下阈值。 |
| `MaCandleType` | `DataType` | `15m` | 均线与交易使用的主周期。 |
| `CciCandleType` | `DataType` | `30m` | CCI 使用的周期。 |
| `MaMethod` | `MaMethod` | `Exponential` | 均线平滑方式（Simple、Exponential、Smoothed、LinearWeighted）。 |
| `MaPriceMode` | `AppliedPriceMode` | `Median` | 均线的价格输入。 |
| `CciPriceMode` | `AppliedPriceMode` | `Typical` | CCI 的价格输入。 |

## 实现细节

- 当 CCI 与均线周期一致时，策略只建立一次订阅，同时把同一批 K 线推送给两个处理函数；否则会额外建立第二个订阅。
- 为模拟 `ma_slow_array[1]`、`ma_slow_array[2]` 与 `ma_fast_array[0]`，策略使用字段缓存指标的最新值，无需手动维护数组。
- 下单量会根据品种的最小交易单位、步长以及最大交易量进行归一化，以减少被拒单的概率。
- 资金管理模块记录最近一次开仓与平仓的价格，从而在 StockSharp 环境中估算盈亏并维护亏损串。

## 与原始 EA 的差异

- MetaTrader 的 `FreeMarginCheck`、`MarginCheck`、`HistorySelect` 等函数由 StockSharp 的投资组合信息和内部计数逻辑替代。
- StockSharp 采用净头寸模型，因此平仓时会一次性扳平该方向的所有仓位。
- 原策略的大量 `Print` 调试信息被移除，改为依赖 StockSharp 自带的日志系统。
