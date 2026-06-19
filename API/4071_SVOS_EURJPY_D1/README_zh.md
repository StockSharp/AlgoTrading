# SVOS EURJPY D1 策略

## 概述
本策略将 MetaTrader 4 上的 **SVOS_EURJPY_D1** 专家顾问移植到 C#。它基于 EURJPY 的日线数据，通过市场状态划分、
指标过滤和蜡烛形态管理仓位。Vertical Horizontal Filter (VHF) 用于识别趋势或震荡行情：当处于趋势状态时，策略依赖
MACD 柱状图 (OSMA) 的斜率；当市场横盘时，则切换为 Stochastic 随机指标。吞没形态以及“晨星/暮星”等蜡烛组合用来
在行情出现反向信号时及时平仓。

## 交易逻辑
- **行情识别**：比较上一根日线的 VHF 值与参数 `VhfThreshold`。高于阈值则启用趋势模块，否则启用震荡模块。
- **趋势确认**：比较 5、20 周期 EMA 与 130 周期 EMA（对应原策略的 6 个月均线）。当满足多头趋势条件时，买入手数
  乘以 `RiskBoost`；满足空头趋势条件时，卖出手数乘以该参数。
- **指标过滤**：
  - 趋势模式下：当 OSMA 为正且递增 (`OSMA[1] > 0` 且 `OSMA[1] > OSMA[2]`) 时做多；当 OSMA 为负且递减时做空。
  - 震荡模式下：当随机指标主线向上穿越信号线时做多，向下穿越时做空。
  - **波动性检查**：上一根日线的标准差必须大于 `StdDevMinimum` 才允许开仓。
- **价格形态**：最近一根完整蜡烛必须不是十字线 (`DojiDivisor`)，并且蜡烛颜色需要与计划方向一致。出现反向吞没或
  晨星/暮星形态时立即平掉对应方向的所有仓位。
- **仓位限制**：当市场为趋势状态时，最多同时持有 `MaxTrendOrders` 个订单；震荡状态下最多 `MaxRangeOrders` 个订单。
- **风险控制**：每笔订单都会设置固定止损和止盈 (`StopLossPips`, `TakeProfitPips`)，并启用 `TrailingStopPips` 定义的追踪
  止损。追踪止损使用日线的最高/最低价进行更新，以贴近原始 MT4 行为。

## 指标说明
- **指数移动平均线 (5, 20, 130)**：确认方向并用于调整仓位规模。
- **Vertical Horizontal Filter**：自定义指标，通过价格区间与累计收盘价变化的比率区分趋势和震荡。
- **MACD (OSMA)**：MACD 主线与信号线的差值决定趋势模式下的进出场。
- **Stochastic 随机指标**：在震荡模式下提供均值回复信号。
- **标准差**：保证只有在波动足够时才会开仓。

## 仓位管理
- 策略通过 `BuyMarket`/`SellMarket` 下单，并在内部记录每笔订单，以便在 StockSharp 的净额结算模式下模拟独立的止损/止盈。
- 当蜡烛的价格范围触及某个订单的止损或止盈时，对应的仓位部分会被立即平仓。
- 追踪止损按照最新日线的最高价（多头）或最低价（空头）移动，保持预设距离。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `LotSize` | 每次开仓的基础手数。 | `0.1` |
| `RiskBoost` | 当趋势成立时对手数的放大倍数。 | `3` |
| `TakeProfitPips` | 止盈距离（点）。 | `350` |
| `StopLossPips` | 止损距离（点）。 | `90` |
| `TrailingStopPips` | 追踪止损距离（点，始终启用）。 | `150` |
| `StochKPeriod` | 随机指标 %K 周期。 | `8` |
| `StochDPeriod` | 随机指标 %D 周期。 | `3` |
| `StochSlowing` | %K 平滑系数。 | `3` |
| `StdDevPeriod` | 标准差窗口长度。 | `20` |
| `StdDevMinimum` | 开仓所需的最小标准差。 | `0.3` |
| `VhfPeriod` | VHF 指标周期。 | `20` |
| `VhfThreshold` | 判断趋势/震荡的阈值。 | `0.4` |
| `MaxTrendOrders` | 趋势模式下允许的最大订单数。 | `4` |
| `MaxRangeOrders` | 震荡模式下允许的最大订单数。 | `2` |
| `MacdFastLength` | MACD 快速 EMA 周期。 | `10` |
| `MacdSlowLength` | MACD 慢速 EMA 周期。 | `25` |
| `MacdSignalLength` | MACD 信号线 EMA 周期。 | `5` |
| `DojiDivisor` | 判断十字线的分母（实体 < 高低差/分母）。 | `8.5` |
| `CandleType` | 使用的蜡烛类型（默认日线）。 | `1 day` |
| `PipSizeOverride` | 自定义点值（`0` 表示根据 `PriceStep` 自动计算）。 | `0` |

## 实现细节
- 原策略在月线级别上使用 6 个月 EMA。移植版本通过日线的 130 周期 EMA 近似该滤波，从而只需要订阅一种时间框架的数据。
- 由于 StockSharp 默认采用净额结算，为了复制 MT4 的逐单管理方式，策略内部维护每笔订单的止损、止盈和追踪止损。
- 追踪止损基于日线高低点更新，若日内发生剧烈反转，实际结果可能与 MT4 的逐笔更新略有差异。
- 点值默认根据 `Security.PriceStep` 计算；若经纪商使用不同报价精度，可通过 `PipSizeOverride` 手动指定。

## 使用建议
1. 将策略绑定到 EURJPY 日线数据，如需其他周期请调整 `CandleType`。
2. 检查点值是否正确识别，必要时设置 `PipSizeOverride`。
3. 根据账户规模调整 `LotSize` 与 `RiskBoost` 等资金管理参数。
4. 先在 StockSharp Designer 或 Runner 中回测/验证，再投入真实环境。
