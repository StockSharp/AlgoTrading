# Live Alligator 策略

该策略利用动态配置的 Alligator 指标和多条 EMA 过滤器来捕捉趋势反转。
当 Alligator 线条改变方向且五条 EMA 确认趋势时开仓。
可选的交易时段过滤器限制在指定时间内入场。
当价格跌破或突破基于 `TrailPeriod` 的平滑移动平均线时平仓。

- **入场条件**
  - Lips 在 Jaw 之上、Teeth 在 Jaw 之下且前一根柱子的 Lips 位于 Jaw 下方时，在空头趋势后开多。
  - Lips 在 Jaw 之下、Teeth 在 Jaw 之上且前一根柱子的 Lips 位于 Jaw 上方时，在多头趋势后开空。
  - 基于收盘价、加权价、典型价、中位价和开盘价的五条 EMA 必须按趋势方向严格排列。
- **出场条件**
  - 价格穿越 `TrailPeriod` 平滑移动平均线。
  - 开仓时可选择设置止损。
- **使用的指标**
  - Alligator 三条线及其基于 SMMA 的跟踪止损。
  - 不同价格类型上的 EMA。

参数可配置 Alligator 基准周期、EMA 确认周期、跟踪周期、止损和交易时间窗口。
