# Last ZZ50 策略

## 概览
Last ZZ50 策略复制了 Vladimir Karputov 在 MetaTrader 上的同名专家顾问。
策略通过 ZigZag 指标跟踪最近的三个枢轴点，并在最后两段 ZigZag 线段的中点位置挂出等待成交的订单。
一旦 ZigZag 结构发生变化，这些订单就会被自动撤销并重新计算，从而紧跟新的波动结构。

## 交易逻辑
- **枢轴识别**：ZigZag 指标（默认深度 12、偏差 5、回退 3）提供最近的 A、B、C 三个枢轴。
- **BC 段订单**：当 B、C 两点更新且最新的 A 枢轴没有否定该方向时，在 `(B + C) / 2` 处挂出订单。
  - BC 段向上则挂多单，向下则挂空单。
  - 依据当前价格与中点的位置关系自动选择限价或止损类型。
- **AB 段订单**：对 AB 段重复同样的中点挂单逻辑，用于捕捉当前波段的回踩。
- **时间过滤**：仅在设定的工作日和时间窗口内交易（默认周一 09:01 至周五 21:01）。
  超出窗口时会撤销所有挂单，并可选择性地平掉持仓。
- **移动止损**：当浮盈超过 `TrailingStop` 与 `TrailingStep` 之和后，策略会启动移动止损，将保护性订单紧随价格移动。

## 风险控制
- 每个订单的数量等于 `LotMultiplier` 与品种最小交易量的乘积。
- 只要 ZigZag 枢轴发生变化，AB 和 BC 两组挂单都会取消并重新计算，避免遗留过期订单。
- 移动止损仅在仓位明显盈利时才会启动，减少震荡行情中过早离场的情况。

## 参数
- `LotMultiplier`：下单时使用的最小交易量倍数。
- `ZigZagDepth`、`ZigZagDeviation`、`ZigZagBackstep`：ZigZag 指标的配置参数。
- `TrailingStopPips`、`TrailingStepPips`：移动止损的距离与触发阈值（以点数表示）。
- `StartDay`、`EndDay`、`StartTime`、`EndTime`：允许交易的日期与时间窗口。
- `CloseOutsideSession`：是否在时段外立即平仓。
- `CandleType`：用于计算 ZigZag 的蜡烛周期（默认 1 小时）。

## 指标
- **ZigZag** – 提供核心枢轴点，是所有挂单及过滤条件的基础。
