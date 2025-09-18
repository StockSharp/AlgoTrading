# Bago EA Classic 策略

该策略是对 `MQL/7656/Bago_ea.mq4` MetaTrader 专家的完整移植。它保持原始趋势交易思想：只有当 EMA 与 RSI 同向突破中性区间时才入场，而 Vegas 隧道（144/169 EMA 组合）既过滤噪音又为分段追踪止损提供参照。

## 交易逻辑

1. **指标准备**
   - 快速与慢速 EMA（`FastPeriod`/`SlowPeriod`，共享 `MaMethod` 与 `MaAppliedPrice`）。
   - 固定周期 144 与 169 的 EMA 组成 Vegas 隧道，设置与主 EMA 保持一致。
   - RSI (`RsiPeriod`, `RsiAppliedPrice`) 使用经典的 50 水平作为确认条件。
   - 所有指标都来自 `CandleType` 指定的蜡烛序列，通过高层 `Bind` 管线同步更新。
2. **穿越状态机**
   - EMA 与 RSI 的上/下穿会设置布尔标记并启动计数器。每个状态在 `CrossEffectiveBars` 根已完成蜡烛后过期，或被反向穿越立即重置，完全复刻 MQL 版本中的计时器。
   - 额外的隧道标记用于追踪收盘价是否跨越 Vegas 隧道，从而决定采用哪种止损模式。
3. **交易时段过滤**
   - 仅在启用的市场时段内下单：伦敦 (07–16)、纽约 (12–21) 与东京 (00–08 以及 23:00 蜡烛)。这些窗口对应原始 EA 的布尔开关。
4. **入场筛选**
   - **多头**：需要 EMA-上穿与 RSI-上穿均为真，并且满足以下任一条件：
     - 蜡烛向上收盘，距离隧道至少 `TunnelBandWidthPips`，但不超过 `TunnelSafeZonePips`；
     - 或者收盘价低于隧道 `TunnelBandWidthPips`，表示自下方反弹。
   - **空头**：使用对称的条件（EMA/RSI 下穿与隧道镜像判断）。
   - 如存在反向持仓，策略会先市价平仓再开新仓，等同于 MetaTrader 中的 `OrderClose` 行为。
5. **仓位管理与退出**
   - 初始止损距离入场 `StopLossPips`。当价格停留在隧道附近时，止损会根据 `StopLossToFiboPips` 额外缓冲重新放置，以模拟 EA 中“fibo” 偏移。
   - 追踪阶段与 EA 的分级止盈对应：止损先移动到隧道 ±(`TrailingStepX` + `StopLossToFiboPips`)，随后在突破隧道后转为 `TrailingStopPips` 的硬性追踪。
   - 当价格分别达到 `TrailingStep1Pips` 与 `TrailingStep2Pips` 时，执行 `PartialClose1Volume` 与 `PartialClose2Volume` 的分批平仓。剩余仓位在第三层 `TrailingStep3Pips` 前由追踪止损控制。
   - 任何反向的 EMA/RSI 穿越都会立即关闭全部头寸。
6. **订单维护**
   - 止损通过 `SellStop`/`BuyStop` 显式创建；每次需要移动止损都会取消旧订单并提交新订单，以对应 MQL 中反复调用的 `OrderModify`。
   - 所有点值换算依赖品种的 `PriceStep`，当报价为 3 或 5 位小数时会自动乘以 10，完全符合 MetaTrader 的 `Point` 处理方式。

## 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `TradeVolume` | decimal | 3 | 每次开仓的总手数。 |
| `StopLossPips` | decimal | 30 | 初始止损距离。 |
| `StopLossToFiboPips` | decimal | 20 | 靠近隧道时重新放置止损的额外缓冲。 |
| `TrailingStopPips` | decimal | 30 | 远离隧道后使用的固定追踪距离。 |
| `TrailingStep1Pips` | decimal | 55 | 第一级盈利阈值（TP1）。 |
| `TrailingStep2Pips` | decimal | 89 | 第二级盈利阈值（TP2）。 |
| `TrailingStep3Pips` | decimal | 144 | 第三级盈利阈值（TP3），随后仅保留追踪止损。 |
| `PartialClose1Volume` | decimal | 1 | 达到第一级盈利时平仓的手数。 |
| `PartialClose2Volume` | decimal | 1 | 达到第二级盈利时平仓的手数。 |
| `CrossEffectiveBars` | int | 2 | 穿越信号保持有效的蜡烛数量。 |
| `TunnelBandWidthPips` | decimal | 5 | 隧道附近的中性区，避免在噪音区域入场。 |
| `TunnelSafeZonePips` | decimal | 120 | 隧道外允许入场的最大距离。 |
| `EnableLondonSession` | bool | true | 启用伦敦时段 07:00–16:00。 |
| `EnableNewYorkSession` | bool | true | 启用纽约时段 12:00–21:00。 |
| `EnableTokyoSession` | bool | false | 启用东京时段 00:00–08:00 以及 23:00 蜡烛。 |
| `FastPeriod` | int | 5 | 快速 EMA 周期。 |
| `SlowPeriod` | int | 12 | 慢速 EMA 周期。 |
| `MaShift` | int | 0 | 移动平均的水平偏移。 |
| `MaMethod` | `MovingAverageType` | Exponential | 移动平均计算方式。 |
| `MaAppliedPrice` | `AppliedPriceType` | Close | 输入 EMA 的价格类型。 |
| `RsiPeriod` | int | 21 | RSI 平滑周期。 |
| `RsiAppliedPrice` | `AppliedPriceType` | Close | 输入 RSI 的价格类型。 |
| `CandleType` | `DataType` | H1 | 使用的蜡烛数据类型。 |

## 实现说明

- 策略完全基于高层蜡烛订阅 API，使用滚动列表模拟 MQL 中的 `Close[1]`、`Close[2]` 索引，并限制在 `HistoryLimit` 之内。
- 状态机与隧道标记完整复制原始 EA 中的逻辑，确保每个信号的“新鲜度”一致。
- `OnStarted` 调用 `StartProtection()`，让 StockSharp 的内建保护机制像 MetaTrader 的硬止损一样守护仓位。
- 代码中的行内注释全部为英文，方便跨语言团队维护。
