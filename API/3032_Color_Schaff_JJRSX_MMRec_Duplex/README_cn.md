# Color Schaff JJRSX MMRec Duplex 策略

## 概述
本策略是 MetaTrader 智能交易系统 `Exp_ColorSchaffJJRSXTrendCycle_MMRec_Duplex` 在 StockSharp 平台上的移植版本。原始机器人将基于 JJRSX 的双重 Schaff Trend Cycle 振荡器与 MMRec（资金管理重算）模块结合，在出现连续亏损时自动降低头寸规模。C# 版本保留了多空双通道结构和灵活的风险参数，并使用平台内的组件实现对 JJRSX 指标的可靠近似。

## 交易逻辑
- 在用户指定的两个时间框架上分别计算振荡器：一个控制做多信号，另一个控制做空信号。每个振荡器使用快慢两条 RSX 动量线，通过 Schaff Trend Cycle 流程进行平滑和归一化，输出范围为 [-100, 100]。
- 当长周期振荡器从上向下穿越 0（`previous > 0` 且 `current <= 0`）时开多仓，原策略将其视为多头反转的起点。若振荡器在上一根柱子上的值为负，则平掉多仓。
- 当短周期振荡器从下向上穿越 0（`previous < 0` 且 `current >= 0`）时开空仓；若上一根柱子的值为正，则平掉空仓。
- `SignalBar` 参数复现了 MQL 的历史柱分析方式，例如 `SignalBar = 1` 时使用前一根完整柱子及再前一根柱子的数值。策略维护滚动的指标历史以模拟 MQL 中的 `CopyBuffer` 调用。

## 资金管理（MMRec）
- 多头与空头分别维护独立的 MMRec 计数器。基础下单量等于 `Strategy.Volume * MM`，其中 `MM` 为默认的仓位放大系数（`LongMm`/`ShortMm`）。
- 每次平仓后记录交易盈亏（基于入场和离场蜡烛的收盘价，与 EA 中通过 `HistorySelect` 统计交易结果的思路一致）。
- 如果最近 `TotalTrigger` 笔交易中至少有 `LossTrigger` 笔亏损，则下一笔对应方向的交易会自动使用降低后的系数 `SmallMm`；当亏损条件不再满足时，恢复默认系数。
- 当仓位反向（多转空或空转多）时，会先对原仓位的盈亏进行结算并更新亏损计数，再计算新仓位的下单量。

## 指标近似
原 EA 依赖自定义的 `ColorSchaffJJRSXTrendCycle` 指标，该指标基于 JJRSX 与 Jurik 平滑库实现。StockSharp 未提供这些组件，因此移植版实现了 `ColorSchaffJjrsxTrendCycleIndicator`：
- 通过轻量级的 `SimpleRsi` 类实现 RSX 动量的近似计算，并按照输入参数进行指数平滑。
- 计算快慢 RSI 曲线的差值获得类似 MACD 的序列，再在循环窗口内做最小值/最大值归一化，并用可配置的系数（默认 0.5）进行二次平滑，以模拟 Schaff Trend Cycle。
- 指标支持与原版相同的价格源（收盘价、开盘价、最高价、最低价、中价、典型价、加权价等），并保留周期与循环参数，方便复现优化流程。

## 参数
| 组别 | 名称 | 说明 |
| --- | --- | --- |
| Long | `LongCandleType` | 多头指标使用的蜡烛类型/时间框架。 |
| Long | `LongTotalTrigger` | 统计 MMRec 时考虑的多头交易数量。 |
| Long | `LongLossTrigger` | 在统计窗口内触发降级系数所需的亏损次数。 |
| Long | `LongSmallMm` | 发生连续亏损后使用的缩减系数。 |
| Long | `LongMm` | 多头默认仓位系数。 |
| Long | `LongEnableOpen` | 是否允许开多仓。 |
| Long | `LongEnableClose` | 是否允许平多仓。 |
| Long | `LongFastLength` | JJRSX 快线近似周期。 |
| Long | `LongSlowLength` | JJRSX 慢线近似周期。 |
| Long | `LongSmooth` | 归一化前的指数平滑长度。 |
| Long | `LongCycleLength` | Schaff 归一化所使用的窗口。 |
| Long | `LongSignalBar` | 分析多头信号时使用的历史偏移。 |
| Long | `LongAppliedPrice` | 多头指标采用的价格类型。 |
| Short | `ShortCandleType` | 空头指标使用的蜡烛类型/时间框架。 |
| Short | `ShortTotalTrigger` | 统计 MMRec 时考虑的空头交易数量。 |
| Short | `ShortLossTrigger` | 在统计窗口内触发降级系数所需的亏损次数（空头）。 |
| Short | `ShortSmallMm` | 连续亏损后空头使用的缩减系数。 |
| Short | `ShortMm` | 空头默认仓位系数。 |
| Short | `ShortEnableOpen` | 是否允许开空仓。 |
| Short | `ShortEnableClose` | 是否允许平空仓。 |
| Short | `ShortFastLength` | 空头 JJRSX 快线近似周期。 |
| Short | `ShortSlowLength` | 空头 JJRSX 慢线近似周期。 |
| Short | `ShortSmooth` | 归一化前的指数平滑长度（空头）。 |
| Short | `ShortCycleLength` | 空头 Schaff 归一化窗口。 |
| Short | `ShortSignalBar` | 分析空头信号时使用的历史偏移。 |
| Short | `ShortAppliedPrice` | 空头指标采用的价格类型。 |

## 实现说明
- 使用 StockSharp 的高层蜡烛订阅与指标绑定，不直接访问指标缓冲区，符合移植规范。
- MQL 版本中的 `StopLoss`/`TakeProfit` 以点数表示，未直接迁移；可根据需要在 StockSharp 中通过 `StartProtection` 或外部风控模块补充。
- 交易结果按蜡烛收盘价计算，保持逻辑确定性并贴近原 EA 的历史交易评估方式。
- 自定义指标公开 `IsFormed` 标志，确保仅在积累足够数据后才触发交易信号，避免初始化阶段的误报。

## 风险提示
尽管移植忠实复现了原策略的逻辑，但由于数据源、执行机制以及 JJRSX 近似方式的差异，实际表现可能不同。务必先在模拟环境中充分测试再投入真实交易。
