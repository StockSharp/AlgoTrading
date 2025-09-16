# Ergodic Ticks Volume 指标策略

该策略对指定周期的蜡烛数据应用 True Strength Index (TSI)，并与指数移动平均线形成的信号线比较。当 TSI 上穿信号线时做多，下穿信号线时做空。

## 参数

- **Candle Type** – 计算所用的蜡烛周期。
- **Short Length** – TSI 的快速平滑周期。
- **Long Length** – TSI 的慢速平滑周期。
- **Signal Length** – 作为信号线的 EMA 周期。

## 逻辑

1. 订阅选定周期的蜡烛。
2. 对每根完成的蜡烛计算 TSI。
3. 通过 EMA 处理 TSI 得到信号线。
4. 当 TSI 上穿信号线时，关闭空头并开多头。
5. 当 TSI 下穿信号线时，关闭多头并开空头。

该策略改编自 MQL 示例 "exp_ergodic_ticks_volume_indicator.mq5"，仅使用 StockSharp 内置指标。
