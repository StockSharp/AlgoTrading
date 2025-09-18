# Forex Profit System 策略
[English](README.md) | [Русский](README_ru.md)

本策略将经典的 MetaTrader 智能交易系统 “Forex Profit System” 移植到 StockSharp 的高级 API 中。它对每根已完成
K线的中位价同时计算三条指数移动平均线（EMA 10、25、50），并叠加 Parabolic SAR 滤波器，用于确认动量突破。
当快速均线穿越慢速均线且 SAR 点已经翻转到同一侧时，策略认为出现了可以跟随的趋势冲量。

## 交易逻辑

1. **指标组合**
   - 所有计算都使用完成K线的中位价，与 MetaTrader 中的 `PRICE_MEDIAN` 输入保持一致。
   - EMA(10) 捕捉短周期动量变化；EMA(25) 与 EMA(50) 则定义趋势方向。
   - Parabolic SAR 步长 0.02、最大值 0.2，确认价格已经站在趋势一侧。
2. **做多条件**
   - EMA(10) > EMA(25) 且 EMA(10) > EMA(50)。
   - 前一根 K 线中 EMA(10) ≤ EMA(50)，即快速均线上穿慢速均线。
   - SAR 值位于收盘价下方。
   - 当前没有持仓，并且策略处于允许交易的状态。
3. **做空条件**
   - EMA(10) < EMA(25) 且 EMA(10) < EMA(50)。
   - 前一根 K 线中 EMA(10) ≥ EMA(50)，即快速均线下穿慢速均线。
   - SAR 位于收盘价上方。
4. **仓位管理**
   - 开仓后立即根据方向分别设置止损与止盈。
   - 当浮动盈利达到设定的触发距离时，启动追踪止损，将保护价拉到距离当前价固定点数的位置。
   - 如果 EMA(10) 方向出现反转，同时浮盈超过最小触发值，则提前离场锁定利润。

## 默认参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | 15 分钟 | 策略处理的 K 线周期。 |
| `FastEmaLength` | 10 | 快速 EMA 的周期。 |
| `MediumEmaLength` | 25 | 中速 EMA 的周期。 |
| `SlowEmaLength` | 50 | 慢速 EMA 的周期。 |
| `SarStep` | 0.02 | Parabolic SAR 初始步长。 |
| `SarMax` | 0.2 | Parabolic SAR 最大步长。 |
| `Volume` | 0.1 | 下单手数/合约数量。 |
| `LongTakeProfitPoints` | 50 | 多单止盈距离（点）。 |
| `ShortTakeProfitPoints` | 50 | 空单止盈距离（点）。 |
| `LongStopLossPoints` | 30 | 多单止损距离（点）。 |
| `ShortStopLossPoints` | 30 | 空单止损距离（点）。 |
| `LongTrailingStopPoints` | 10 | 多单追踪止损触发距离。 |
| `ShortTrailingStopPoints` | 10 | 空单追踪止损触发距离。 |
| `LongProfitTriggerPoints` | 10 | 多单根据 EMA 反转退出所需的最小浮盈。 |
| `ShortProfitTriggerPoints` | 5 | 空单根据 EMA 反转退出所需的最小浮盈。 |

## 实现细节

- 使用高级 API 的 K 线订阅与指标绑定来驱动逻辑，无需处理底层盘口数据。
- 所有以“点”为单位的参数都会根据 `PriceStep` 自动换算成真实价格距离；若没有价格步长，策略直接使用原始值。
- 通过 `SetStopLoss` 与 `SetTakeProfit` 在下单后立即为最终仓位设置保护，兼容部分成交的场景。
- 分别记录最近一次多头与空头的入场价，用于计算追踪止损和 EMA 反转退出条件。
- 仅在 K 线收盘时处理信号，不会出现重绘现象，行为与 MetaTrader 中 `start()` 函数的实现保持一致。

## 使用建议

- 推荐应用于流动性良好的外汇或差价合约，默认 15 分钟周期；可根据品种调整 EMA 周期和风险参数。
- 若品种的波动或最小跳动价不同，请对应修改止损、止盈、追踪止损和利润触发参数。
- 当市场在特定时段点差扩大时，可叠加时段或点差过滤器，以确保策略在合理的交易成本下运行。
