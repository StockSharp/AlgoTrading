# ASCV 枢轴突破策略

## 概述

ASCV 枢轴突破策略是 MetaTrader 4 专家顾问 “ASCV”（`Avpb.mq4`）在 StockSharp 平台上的高级移植版本。原始 EA 同时依赖两个封闭指标（ASCTrend1sig 与 BrainTrend1Sig）、标准差过滤器、日内枢轴水平以及成交量加速来在限定时段内捕捉突破延续。由于专有指标无法直接移植，本策略使用移动平均、随机指标以及日枢轴的组合来复现其决策流程，并完整保留仓位管理和保护逻辑。

## 交易逻辑

1. **交易时段过滤**：仅在设定的开始/结束小时之间允许开仓（默认 02:00–20:00）。每到整点会重置当小时的入场标志，对应 MQL 中 `Minute()==0` 的处理。
2. **波动率门槛**：所选时间框架上的标准差必须高于阈值，模拟原版 `iStdDev` 对活跃行情的要求。
3. **趋势确认**：快慢两条简单移动平均线替代 ASCTrend/BrainTrend 的方向判断。做多需满足快速均线高于慢速均线且收盘价高于当日枢轴；做空条件相反。
4. **动量确认**：随机指标保证突破时 `%K-%D` 为正（多头）或为负（空头）。该差值的绝对值同时作为动量退出条件，对应原始 EA 中的 `dstob/dstos` 逻辑。
5. **成交量加速**：当前 K 线成交量需相对前一根增加至少阈值（默认 30），重现 `Volume[0]-Volume[1]` 检查。
6. **下单方式**：使用固定手数的市价单 `BuyMarket` / `SellMarket`，每个小时内同方向仅允许一次入场。
7. **止损与止盈**：优先将止损设置在最近的枢轴支撑/阻力（S1/S2 或 R1/R2）。若距离不足，则使用以价格步长表示的备用距离。止盈目标遵循同样的层级：多头优先 R2→R1→Pivot，空头优先 S2→S1→Pivot，缺失时退化为备用距离。
8. **动态管理**：随机指标差值触发动量式离场，同时以价格步长计算的跟踪止损复制 EA 的移动止损行为。

## 参数

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 用于指标与信号的蜡烛类型。 | 15 分钟 K 线 |
| `StartHour` / `EndHour` | 允许交易的起止小时（含边界）。 | 2 / 20 |
| `FastMaLength` | 快速 SMA 长度。 | 10 |
| `SlowMaLength` | 慢速 SMA 长度。 | 40 |
| `StdDevLength` | 标准差窗口长度。 | 10 |
| `StdDevThreshold` | 最小标准差阈值。 | 0.0005 |
| `VolumeDeltaThreshold` | 当前与上一根 K 线成交量的最小增量。 | 30 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | 随机指标周期参数。 | 5 / 3 / 3 |
| `StochasticExitDelta` | 触发动量离场的 `%K-%D` 绝对阈值。 | 5 |
| `TrailingStopSteps` | 跟踪止损距离（以价格步长计）。 | 30 |
| `MinPivotDistanceSteps` | 使用枢轴目标时要求的最小距离。 | 50 |
| `StopFallbackSteps` | 枢轴过近时的备用止损距离。 | 33 |
| `TakeProfitBufferSteps` | 枢轴过近时的备用止盈距离。 | 50 |
| `OrderVolume` | 每次下单的数量。 | 1 |

所有距离均以交易品种的价格步长表示，便于适配不同交易所的最小变动价位。

## 实现说明

- 采用 `SubscribeCandles().BindEx(...)` 的高阶 API 结构，未将指标添加到 `Strategy.Indicators` 中，符合项目规范。
- 枢轴水平在每日切换时根据上一交易日的最高价、最低价与收盘价重新计算。首个交易日仅收集数据，第二天才开始信号判断。
- 调用了 `StartProtection()` 以在断线时自动保护仓位，与 EA 的安全机制一致。
- C# 代码中提供了英文注释和 XML 文档，说明各逻辑与原始 MQL 的对应关系。
- 止损与止盈通过 `SetStopLoss` 和 `SetTakeProfit` 下发，内部自动转换为步长单位，兼容不同合约规格。

## 使用建议

1. 建议在提供真实成交量的品种上运行策略，成交量过滤是核心条件之一。
2. 优化时优先关注波动率 (`StdDevThreshold`) 和成交量 (`VolumeDeltaThreshold`) 参数，它们对信号密度影响最大。
3. 对波动较大的品种可适当增大 `MinPivotDistanceSteps`，防止过早触发止盈。
4. 若随机指标差值过于敏感，可增大 `StochasticExitDelta` 让跟踪止损成为主要的离场方式。

## 文件列表

- `CS/AscvStrategy.cs` —— 策略实现。
- `README.md` —— 英文说明。
- `README_ru.md` —— 俄文说明。
- `README_cn.md` —— 中文说明（当前文件）。
