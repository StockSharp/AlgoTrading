# 波动枢轴策略

## 概述
波动枢轴策略是原始 **Exp_VolatilityPivot.mq5** 智能交易程序的 StockSharp 高层 API 版本。策略重建了自定义的波动枢轴指标，通过 ATR 波动率或固定价差绘制一对自适应的跟踪线，并在趋势翻转时生成单根信号箭头。交易者可以选择顺势模式 (`WithTrend`) 追随突破，也可以选择逆势模式 (`CounterTrend`) 在枢轴点附近做反向交易。

本实现完全基于 `CandleType` 提供的已完成 K 线。ATR 模式会将长度为 `AtrPeriod` 的 `AverageTrueRange` 值经由 `SmoothingPeriod` 长度的指数移动平均进行平滑，并与 `AtrMultiplier` 相乘；价格偏移模式则直接使用 `DeltaPrice` 距离。最终得到的上/下枢轴线用于定义多空趋势与出入场条件。

## 市场数据与指标
- **基础周期 (`CandleType`)** – 所有计算均在该周期上完成，默认值为 4 小时，与原版 EA 保持一致。
- **ATR + EMA 平滑** – 当 `PivotMode` 为 `Atr` 时，策略先计算 ATR，然后使用指数移动平均平滑，再乘以倍数得到波动宽度。
- **固定价差模式** – 当 `PivotMode` 为 `PriceDeviation` 时，枢轴线之间的距离固定为 `DeltaPrice`，适合不希望使用波动率自适应的场景。
- **枢轴状态跟踪** – 策略记录当前的多头/空头轨迹，仅在轨迹从无到有的翻转时触发一次信号，完全对应 MQL 指标的缓冲区含义。

## 交易逻辑
1. **枢轴计算** – 每根完成的 K 线都会更新新的跟踪止损价位。若收盘价高于止损，则视为多头轨迹；若低于止损，则视为空头轨迹。
2. **信号检测** – 当多头（空头）轨迹在上一根不存在、当前根激活时，产生新的做多（做空）信号。`SignalBar` 参数用于延迟执行 N 根已完成 K 线，完全还原原始 EA 的 “信号柱” 逻辑。
3. **方向选择 (`TradeDirection`)** – `WithTrend` 模式下，策略在多头信号开多、在空头信号开空；`CounterTrend` 模式则将信号反向使用。
4. **开仓许可** – `EnableBuyEntries` 与 `EnableSellEntries` 控制是否允许建立新的多头或空头仓位。
5. **平仓许可** – `AllowLongExits` 与 `AllowShortExits` 控制是否允许因对立信号或持续轨迹而平掉已有仓位。
6. **仓位调整** – 策略目标仓位为多头 `+Volume`、空头 `-Volume`、或空仓 `0`。下单数量会自动覆盖相反方向持仓，再建立新的净仓位。
7. **保护性止损/止盈** – `StopLoss` 与 `TakeProfit`（以绝对价格单位表示）会在每根完成的 K 线上检测。一旦高/低价触及对应水平，策略立即离场。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 进行指标计算和执行的 K 线序列。 | 4 小时 |
| `AtrPeriod` | ATR 计算长度。 | 100 |
| `SmoothingPeriod` | ATR 平滑用的 EMA 长度。 | 10 |
| `AtrMultiplier` | 平滑后 ATR 的乘数。 | 3.0 |
| `DeltaPrice` | `PriceDeviation` 模式下的固定价差。 | 0.002 |
| `PivotMode` | 选择 ATR 或固定价差模式。 | `Atr` |
| `TradeDirection` | 选择顺势 (`WithTrend`) 或逆势 (`CounterTrend`)。 | `WithTrend` |
| `SignalBar` | 信号延迟的完成 K 线数量。 | 1 |
| `EnableBuyEntries` | 是否允许开多。 | `true` |
| `EnableSellEntries` | 是否允许开空。 | `true` |
| `AllowLongExits` | 是否允许在看空条件下平多。 | `true` |
| `AllowShortExits` | 是否允许在看多条件下平空。 | `true` |
| `StopLoss` | 绝对价位的止损距离，0 表示关闭。 | 0 |
| `TakeProfit` | 绝对价位的止盈距离，0 表示关闭。 | 0 |

> **提示：** 实际下单数量由 StockSharp 的 `Strategy.Volume` 属性决定。启动策略前请先设置为合适的合约/股数。

## 使用建议
1. 绑定目标 `Security`、`Portfolio`，并将 `Volume` 设置为期望的交易手数。
2. 确保数据源能够提供所选 `CandleType` 的完整历史与实时数据，否则 ATR 平滑与信号延迟将无法形成。
3. 根据市场特性选择 `PivotMode`：ATR 模式更具弹性，固定价差模式则提供固定的止损距离。
4. 调整 `SignalBar` 以匹配原始 EA 的节奏（默认延迟 1 根完成 K 线，设置为 0 即在最近一根完成 K 线上执行）。
5. 使用 `StopLoss`/`TakeProfit` 时请根据品种波动性设置合适的绝对价格距离。
6. 关注日志输出，了解每次进场、离场及保护性止损触发的原因。

## 与原版 EA 的差异
- 移除了基于余额或可用保证金的资金管理参数，持仓规模完全由 `Strategy.Volume` 控制。
- 不再需要 MQL 辅助库中的最大滑点与时间同步逻辑，策略始终使用基于完成 K 线的市价单。
- 省略了全局变量、通知以及手动历史加载等附加功能。
- 保护性止损/止盈采用 K 线级别的检测，不在盘中逐 tick 下单。

## 推荐扩展
- 添加交易时段过滤或波动率过滤，以在流动性较差的时段暂停交易。
- 将计算得到的枢轴线绘制到图表，或基于枢轴线实现动态追踪止损。
- 若同时交易多个品种，可进一步叠加组合层面的风险控制。
