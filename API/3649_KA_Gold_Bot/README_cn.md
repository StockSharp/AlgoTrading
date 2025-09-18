# KA-Gold Bot 策略

**KA-Gold Bot** 策略移植自 MetaTrader 专家顾问 “KA-Gold Bot”。策略基于自定义 Keltner 通道的突破，并结合 EMA 趋势过滤器。移植版本使用 StockSharp 的高级蜡烛订阅和参数系统，保持可视化配置与优化能力。

## 交易逻辑

- 计算三条指数移动平均线：
  - EMA(10)：用于快速动量确认。
  - EMA(200)：作为中期趋势过滤器。
  - EMA(period)：通道中线，同时用相同周期对蜡烛振幅 (High-Low) 做简单平均。
- 通道上下轨通过对振幅做 SMA 得到：
  - 上轨 = EMA(period) + SMA(high-low, period)。
  - 下轨 = EMA(period) − SMA(high-low, period)。
- **做多条件**（上一根完成蜡烛）：
  - 收盘价高于上轨。
  - 收盘价高于 EMA(200)。
  - EMA(10) 从上一根上轨下方穿越到当前上轨上方。
- **做空条件** 对称：
  - 收盘价低于下轨。
  - 收盘价低于 EMA(200)。
  - EMA(10) 从上一根下轨上方穿越到当前下轨下方。
- 仅在持仓为空时允许开新仓。

## 仓位管理

- **固定手数模式**：直接使用 `BaseVolume`。
- **风险百分比模式**：当 `UseRiskPercent = true` 时，读取 `Portfolio.CurrentValue`（或 `Portfolio.BeginValue`）并乘以 `RiskPercent`，再按 100000 的 MetaTrader 约定缩放，最后按照 `BaseVolume` 和交易品种的 `VolumeStep`/`MinVolume`/`MaxVolume` 进行取整。若账户权益不可用则退回固定手数。

## 风险控制

- 止损与止盈以点（pip）输入，通过 `PriceStep` 换算为价格偏移；对于三位或五位报价的外汇品种使用 `pip = step × 10` 的规则。
- 建仓后立即提交对应数量的止损与止盈委托，并在持仓变化时同步调整。
- 当浮动利润达到 `TrailingTriggerPips` 时启动跟踪止损：
  - 多单保持 `TrailingStopPips` 的固定距离；
  - 空单在价格上方保持同样距离；
  - 只有当改进幅度超过 `TrailingStepPips` 时才移动止损，避免频繁触发。
- 平仓后自动撤销所有保护性委托。

## 时间与点差过滤

- `UseTimeFilter`、`StartHour`、`StartMinute`、`EndHour`、`EndMinute` 控制交易时段，遵循 [开始, 结束) 区间。若结束时间早于开始时间则表示跨越午夜。
- 点差过滤器以 `BestAskPrice - BestBidPrice` 计算当前点差并换算成价格步长，与 `MaxSpreadPoints` 比较。若无盘口数据则跳过检查。

## 实现要点

- 通过 `SubscribeCandles().Bind(...)` 订阅蜡烛，EMA(10) 与 EMA(200) 直接由绑定返回；通道 EMA 与范围平均在处理函数内部更新，无需调用 `GetValue`。
- 仅保存最近两根蜡烛及其指标值，以复现 MetaTrader 中 `CopyBuffer` 的移位逻辑。
- 止损与跟踪使用 `BuyStop`、`SellStop`、`BuyLimit`、`SellLimit` 等高级订单方法，效果等同于 MetaTrader 的 `PositionModify`。
- 风险百分比依赖于组合权益数值，若获取失败会自动切换到固定手数。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `KeltnerPeriod` | 通道 EMA 及范围平滑周期。 | 50 |
| `FastEmaPeriod` | 快速 EMA 周期。 | 10 |
| `SlowEmaPeriod` | 慢速 EMA 周期。 | 200 |
| `BaseVolume` | 基础手数。 | 0.01 |
| `UseRiskPercent` | 启用风险百分比模式。 | true |
| `RiskPercent` | 每笔交易占用的资本百分比。 | 1 |
| `StopLossPips` | 止损距离（点）。 | 500 |
| `TakeProfitPips` | 止盈距离（点，0 表示关闭）。 | 500 |
| `TrailingTriggerPips` | 启动跟踪止损的利润阈值。 | 300 |
| `TrailingStopPips` | 跟踪止损与价格的距离。 | 300 |
| `TrailingStepPips` | 每次移动止损的最小改进幅度。 | 100 |
| `UseTimeFilter` | 是否启用交易时段过滤。 | true |
| `StartHour`, `StartMinute` | 开始时间。 | 02:30 |
| `EndHour`, `EndMinute` | 结束时间（不含）。 | 21:00 |
| `MaxSpreadPoints` | 最大允许点差（价格步长数，0 表示忽略）。 | 65 |
| `CandleType` | 信号蜡烛的周期。 | 5 分钟 |

## 与原版的差异

- 跟踪止损通过交易所停损单实现，行为等同于 MetaTrader 中的 `PositionModify`。
- 通道宽度使用 `SMA(high-low)` 计算，与原始公式保持一致。
- 资金管理读取组合权益而非 Free Margin，理念仍是按账户规模分配风险。
- 若缺少买卖盘报价，点差过滤会自动跳过，对应原策略的浮动点差模式。

## 使用建议

- 原策略针对 XAUUSD 调校，应用到其他品种前请优化 EMA 周期与通道宽度。
- 启用风险百分比时，请确保组合窗口能提供最新权益数据。
- 参数以外汇点值为基础，请确认标的的价格步长与小数位符合预期。
