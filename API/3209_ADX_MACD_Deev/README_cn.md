# ADX MACD Deev 策略

## 概述
**ADX MACD Deev Strategy** 是原版 MetaTrader 智能交易系统的 StockSharp 版本。它通过 Average Directional Index (ADX) 衡量趋势强度，并结合 Moving Average Convergence Divergence (MACD) 的动量信号。当两个指标同时确认方向时才开仓，并可选用移动止损和分批止盈来保护利润。

## 工作原理
1. **指标准备**
   - ADX 使用可配置的周期计算，策略保存最近的 ADX 数值，并要求在指定的条数内持续上升（做多）或持续下降（做空）。
   - MACD 使用可配置的快慢 EMA 和信号线。只有当 MACD 柱状图与信号线在指定的条数内同步移动时才认为出现有效趋势。
2. **入场逻辑**
   - **做多**：MACD 柱状图大于 `MACD Minimum (pips)` 阈值，柱状图与信号线连续上升，同时 ADX 高于最小值并继续上升。
   - **做空**：MACD 柱状图低于负阈值，柱状图与信号线连续下降，同时 ADX 高于最小值并持续下降。
   - 策略一次仅持有一笔仓位。
3. **风险管理**
   - 初始止损与止盈使用 `PriceStep` 换算成价格差距，距离由参数中的 pips 指定。
   - 启用移动止损后，当价格推进 `Trailing Stop + Trailing Step` pips 时，止损会向盈利方向移动。
   - 开启 `Take Half Profit` 后，在触发止盈价位时会平掉当前仓位的一半，剩余部分继续由移动止损管理。

## 参数
| 分组 | 名称 | 说明 |
| --- | --- | --- |
| Trading | Order Volume | 每次市价单的交易量。 |
| Risk | Stop Loss (pips) | 初始止损距离。 |
| Risk | Take Profit (pips) | 初始止盈距离。 |
| Risk | Trailing Stop (pips) | 移动止损距离，为 0 时禁用。 |
| Risk | Trailing Step (pips) | 每次调整移动止损前需要的额外价格变化。 |
| Risk | Take Half Profit | 是否在止盈时分批平仓。 |
| Indicators | ADX Period | ADX 平滑周期。 |
| Indicators | ADX Bars Interval | 要求 ADX 同方向变化的最近条数。 |
| Indicators | ADX Minimum | 允许入场的最低 ADX 值。 |
| Indicators | MACD Fast EMA | MACD 快速 EMA 周期。 |
| Indicators | MACD Slow EMA | MACD 慢速 EMA 周期。 |
| Indicators | MACD Signal EMA | MACD 信号线周期。 |
| Indicators | MACD Bars Interval | MACD 需要连续同向的条数。 |
| Indicators | MACD Minimum (pips) | MACD 最小强度，按 pips 表示。 |
| General | Candle Type | 用于计算的 K 线类型或周期。 |

## 使用提示
- 请确保标的的 `PriceStep` 不为零，否则基于 pips 的阈值会退化为直接比较原始 MACD 数值。
- 分批止盈时的数量会根据 `VolumeStep` 进行向下取整。
- 移动止损仅在 K 线收盘后评估。
- 策略使用高层 API 绑定 (`SubscribeCandles().BindEx(...)`)，无需手动获取指标缓存。
