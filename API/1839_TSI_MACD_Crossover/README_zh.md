# TSI MACD Crossover Strategy
[English](README.md) | [Русский](README_ru.md)

该策略基于 True Strength Index (TSI) 及其指数移动平均线信号线的交叉系统。

默认订阅4小时K线，根据可配置的快慢平滑周期计算TSI，并使用额外的EMA生成信号线。当TSI向上穿越信号线时开多头；当TSI向下穿越信号线时开空头。相反方向的交叉会自动平仓现有头寸。

- 指标: True Strength Index, Exponential Moving Average
- 参数:
  - `CandleType` – 处理的K线类型。
  - `LongLength` – TSI的慢周期。
  - `ShortLength` – TSI的快周期。
  - `SignalLength` – 信号线EMA的周期。
