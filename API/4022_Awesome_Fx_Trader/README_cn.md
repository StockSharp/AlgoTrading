# Awesome Fx Trader

该策略复刻了 `MQL/8539` 中的 MetaTrader 方案，原始文件包括自定义指标 **AwesomeFxTradera.mq4** 与 **t_ma.mq4**。前者通过比较当前值与上一根柱体来给 Bill Williams Awesome Oscillator 柱状体着色，后者则在图表上绘制 34 周期的线性加权移动平均线（LWMA）及其 6 周期简单移动平均线平滑曲线。移植到 StockSharp 后，我们保持相同的计算方式，并把颜色变化转换成交易信号。

## 原始 MQL 逻辑

1. **AwesomeFxTradera.mq4** 对 **开盘价** 分别计算 8 周期与 13 周期的指数移动平均线，它们的差值存入 `ExtBuffer0`。当当前值高于上一根柱体时缓冲区显示为绿色，低于则显示为红色，用来表示动量是否在加强。
2. **t_ma.mq4** 绘制开盘价的 34 周期 LWMA（`ExtMapBuffer1`），同时对该 LWMA 的数值再做 6 周期简单平均（`ExtMapBuffer2`）以获得平滑曲线，判断趋势是否加速或减速。

因此在 MetaTrader 中，当振荡器位于零轴之上且数值持续上升、同时价格运行在平滑 LWMA 之上时就被视为多头动量；反之则是空头动量。

## StockSharp 实现

`AwesomeFxTraderStrategy` 订阅可配置的 K 线类型（默认 **15 分钟**），并使用蜡烛的开盘价驱动所有指标，以保持与原始缓冲区完全一致。

1. 每根已完成的蜡烛都会重新计算快慢 EMA，其差值还原出振荡柱状体。
2. 34 周期 LWMA 追踪趋势，6 周期 SMA 对 LWMA 进行平滑，比较两者可以判断趋势曲线的方向。
3. 通过比较当前与上一根柱子的振荡值，重建 MQL 中的 `bool up` 颜色逻辑。
4. **入场规则**：
   - 当振荡器为正值、当前柱体高于上一柱体且 LWMA 位于平滑线上方时开多。
   - 当振荡器为负值、当前柱体低于上一柱体且 LWMA 位于平滑线下方时开空。
5. **离场/反手规则**：出现相反信号时直接反转仓位。下单数量会自动加上当前仓位的绝对值，以确保先平掉旧仓再建立新方向。

原始代码未定义额外的止损或止盈，因此此版本完全依赖动量翻转离场。日志会记录每次触发交易时的指标读数。

## 参数

| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `FastEmaPeriod` | 8 | 复制振荡器所用的快 EMA 周期。 |
| `SlowEmaPeriod` | 13 | 振荡器所用的慢 EMA 周期。 |
| `TrendLwmaPeriod` | 34 | 取自 `t_ma.mq4` 的趋势 LWMA 周期。 |
| `TrendSmoothingPeriod` | 6 | 对 LWMA 数值做平滑的 SMA 窗口长度。 |
| `CandleType` | 15 分钟 K 线 | 用于所有计算的蜡烛数据类型。 |

所有参数均通过 `StrategyParam` 提供显示名称、优化区间，便于在界面中调整或回测。

## 文件映射

| MetaTrader 文件 | StockSharp 对应文件 | 备注 |
| --- | --- | --- |
| `MQL/8539/AwesomeFxTradera.mq4` | `CS/AwesomeFxTraderStrategy.cs` | 复刻基于开盘价的双 EMA 振荡器及其颜色判定。 |
| `MQL/8539/t_ma.mq4` | `CS/AwesomeFxTraderStrategy.cs` | 实现 34 周期 LWMA 与 6 周期 SMA 平滑的趋势过滤器。 |

按需求未创建 Python 版本。
