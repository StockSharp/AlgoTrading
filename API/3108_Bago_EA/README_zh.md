# Bago EA 策略

本策略复刻 MetaTrader 上的 “Bago EA” 专家。它利用 EMA 与 RSI 的同向突破捕捉趋势行情，并借助 Vegas 隧道（144/169 EMA 组合）来过滤噪音与设置分层追踪止损。

## 交易逻辑

1. **指标准备**
   - 两条 EMA（周期 `FastPeriod` 与 `SlowPeriod`，方法 `MaMethod`，价格 `MaAppliedPrice`）。
   - Vegas 隧道 EMA（周期 144 和 169，沿用相同方法/价格）。
   - RSI（周期 `RsiPeriod`，价格 `RsiAppliedPrice`）。
   - 点值换算依据 `PriceStep`，对 3/5 位报价执行与原版相同的调整。
2. **状态管理**
   - 记录 EMA 上/下穿与 RSI 上破/下破 50 水平的状态，每个状态最多保持 `CrossEffectiveBars` 根 K 线，或被反向信号立即替换。
   - 额外的隧道状态标记价格是否穿越 Vegas 隧道。
3. **入场条件**
   - **做多**：EMA 与 RSI 均给出向上突破信号，并且价格满足以下任意条件：
     - 收盘价高于隧道至少 `TunnelBandWidthPips`，但不超过 `TunnelSafeZonePips`，且当根 K 线为阳线；
     - 收盘价低于隧道 `TunnelBandWidthPips`，表示自下方向上的反弹。
   - **做空**：完全对称的反向条件。
   - 只有在允许的交易时段内才会开仓：伦敦 07–16 点、纽约 12–21 点、东京 00–08 点，或任意在 23:00 之后收盘的 K 线。
4. **下单处理**
   - 按 `TradeVolume` 下单；若当前持有反向头寸，会先平仓再反向开单。
   - 初始止损距离为 `StopLossPips`，若需要将止损放置在隧道附近，会额外使用 `StopLossToFiboPips` 的缓冲。
5. **追踪与分批离场**
   - 价格远离隧道后，止损按以下规则移动：
     - 当仍在隧道附近时，止损被设置在 `隧道 ± (TrailingStepX + StopLossToFibo)`。
     - 当行情顺利向外发展时，按 `TrailingStopPips` 的固定距离进行追踪。
   - 当价格到达 `TrailingStep1Pips` 与 `TrailingStep2Pips` 时，分别平掉 `PartialClose1Volume`、`PartialClose2Volume` 的仓位。
   - 一旦出现反向 EMA 或 RSI 信号，立即全量离场。
6. **止损订单**
   - 防护止损通过 `SellStop` / `BuyStop` 自动维护，平仓后会立即撤销。

## 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `TradeVolume` | decimal | 3 | 下单手数。 |
| `StopLossPips` | decimal | 30 | 初始止损距离。 |
| `StopLossToFiboPips` | decimal | 20 | 在隧道附近停留时的附加缓冲距离。 |
| `TrailingStopPips` | decimal | 30 | 离开隧道后追踪止损的距离。 |
| `TrailingStep1Pips` | decimal | 55 | 第一层盈利目标，触发部分平仓与止损上移。 |
| `TrailingStep2Pips` | decimal | 89 | 第二层盈利目标。 |
| `TrailingStep3Pips` | decimal | 144 | 第三层盈利目标，随后进入纯追踪模式。 |
| `PartialClose1Volume` | decimal | 1 | 第一步的平仓手数。 |
| `PartialClose2Volume` | decimal | 1 | 第二步的平仓手数。 |
| `CrossEffectiveBars` | int | 2 | 信号保持有效的 K 线数量。 |
| `TunnelBandWidthPips` | decimal | 5 | 隧道附近的禁止交易带宽。 |
| `TunnelSafeZonePips` | decimal | 120 | 隧道外的安全距离上限。 |
| `EnableLondonSession` | bool | true | 是否启用伦敦时段。 |
| `EnableNewYorkSession` | bool | true | 是否启用纽约时段。 |
| `EnableTokyoSession` | bool | false | 是否启用东京时段。 |
| `FastPeriod` | int | 5 | 快速 EMA 周期。 |
| `SlowPeriod` | int | 12 | 慢速 EMA 周期。 |
| `MaShift` | int | 0 | EMA 的水平偏移。 |
| `MaMethod` | `MovingAverageType` | Exponential | EMA 的平滑方式。 |
| `MaAppliedPrice` | `AppliedPriceType` | Close | EMA 使用的价格类型。 |
| `RsiPeriod` | int | 21 | RSI 周期。 |
| `RsiAppliedPrice` | `AppliedPriceType` | Close | RSI 使用的价格类型。 |
| `CandleType` | `DataType` | H1 | 使用的 K 线级别。 |

## 说明

- 指标状态在非交易时段也会持续更新，与原版 EA 一致。
- 防护止损通过高层 API 维护，等价于 MetaTrader 中的 `PositionModify` 逻辑。
- 代码遵循仓库规范：制表符缩进、英文注释、不调用指标的 `GetValue`/`GetCurrentValue`。
