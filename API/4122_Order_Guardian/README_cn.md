# 4122 Order Guardian

## 概述
本策略是 MetaTrader 专家顾问 `MQL/9210/OrderGuardian.mq4` 的 StockSharp 高层 API 迁移版。它专门守护已经存在的仓位，不断重新计算止盈和止损价位，当行情触发任意一条防护线时立刻按市价平仓，从而复刻原始 EA 的行为。

实现尽可能沿用 MQL 版本的参数名称和默认值。由于 StockSharp 无法枚举 MetaTrader 的趋势线或通道对象，策略改为提供等效的手动价格输入，并且可以在运行过程中实时修改。同时还支持在图表上绘制参考线和输出状态文字，以还原原脚本的视觉反馈。

## 策略逻辑
1. **数据处理**——订阅所选蜡烛类型，只在收盘后的完整蜡烛上执行逻辑，避免盘中噪声。
2. **价位计算**
   - *包络线模式*：对移动平均线施加设定的偏移量，并乘以 `(1 + 偏移百分比)`，生成多空共用的止盈/止损价位。
   - *手动模式*：单独的参数提供多头与空头的绝对价位。
   - *Parabolic SAR*：每根完成的蜡烛都会取一次 SAR 数值，并根据设定的偏移量向前回溯若干根蜡烛，两端共用同一个止损价。
3. **仓位管理**——当持有多头时，用蜡烛的高/低点比较当前止盈和止损；一旦突破即用市价单全部平仓。空头逻辑与之对称。
4. **可视化反馈**——启用后会在图表上绘制连接最近两根蜡烛收盘价的水平线，显示实时的防护价位。当价位变化时会输出类似 `S/L @ ...   T/P @ ...` 的日志，效果与原始 `Comment()` 信息相同。

## 参数
| 参数 | 说明 |
|------|------|
| `CandleType` | 策略处理的蜡烛类型。 |
| `TakeProfitMethod` | 止盈来源：`Envelope`（移动平均偏移）或 `ManualLine`（手动价格）。 |
| `StopLossMethod` | 止损来源：`Envelope`、`ManualLine` 或 `ParabolicSar`。 |
| `TakeProfitPeriod` | 止盈移动平均的周期。 |
| `StopLossPeriod` | 止损移动平均的周期。 |
| `TakeProfitMaMethod` | 止盈移动平均算法（简单、指数、平滑、线性加权）。 |
| `StopLossMaMethod` | 止损移动平均算法。 |
| `TakeProfitPriceType` | 提供给止盈移动平均的价格类型。 |
| `StopLossPriceType` | 提供给止损移动平均的价格类型。 |
| `TakeProfitDeviation` | 在偏移后的止盈移动平均上附加的百分比。 |
| `StopLossDeviation` | 在偏移后的止损移动平均上附加的百分比。 |
| `TakeProfitShift` | 止盈移动平均回溯的完整蜡烛数量。 |
| `StopLossShift` | 止损移动平均或 SAR 回溯的完整蜡烛数量；若选择 `ParabolicSar` 将自动至少为 1。 |
| `ManualTakeProfitLong` | 多头手动止盈价（0 表示禁用）。 |
| `ManualTakeProfitShort` | 空头手动止盈价（0 表示禁用）。 |
| `ManualStopLossLong` | 多头手动止损价（0 表示禁用）。 |
| `ManualStopLossShort` | 空头手动止损价（0 表示禁用）。 |
| `SarStep` | Parabolic SAR 的加速步长。 |
| `SarMaximum` | Parabolic SAR 的最大加速因子。 |
| `ShowLines` | 是否在图表上绘制参考线。 |

## 使用说明
- 策略 **不会** 新开仓位。可搭配其他交易策略或手动下单后运行，用于守护现有仓位。
- 手动价位为实时参数，可在策略运行时直接修改，效果相当于移动图表对象。
- 包络线模式的多空价位相同。偏移为正时产生高于均线的目标，偏移为负时产生低于均线的目标。
- Parabolic SAR 模式沿用原策略的设计，默认回溯一根蜡烛以避免使用未完成的指标数值。
- 最新的状态字符串可以通过 `StatusLine` 属性获取，方便仪表盘或日志系统使用。

## 与 MetaTrader 版本的差异
- 由于无法读取图表上的趋势线或通道，对应功能改为手动输入价格。
- 触发时按策略整体仓位平仓，不再逐个订单处理。
- 图表参考线使用 StockSharp 的绘图接口实现，但仍然在每根完成蜡烛后刷新，与原脚本一致。
