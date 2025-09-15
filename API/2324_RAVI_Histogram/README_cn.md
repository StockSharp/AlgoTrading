# RAVI Histogram 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 的 RAVI Histogram 专家移植到 StockSharp。RAVI 指标通过比较快 EMA 与慢 EMA 的百分比差异来衡量趋势强度，并与上下阈值比较以决定交易。

当 RAVI 高于上限时，被视为多头趋势：若允许，将平掉空头并开多。当 RAVI 低于下限时，策略关闭多头并可开空。默认使用四小时 K 线。

## 细节

- **入场条件**：
  - **多头**：RAVI 向上突破 `UpLevel`。
  - **空头**：RAVI 向下跌破 `DownLevel`。
- **方向**：可做多也可做空。
- **出场条件**：
  - RAVI 产生相反信号时平仓。
- **止损**：无。
- **过滤器**：无。
- **时间框架**：默认 4 小时。
- **参数**：
  - `FastLength` 与 `SlowLength` — 用于计算 RAVI 的 EMA 周期。
  - `UpLevel` 与 `DownLevel` — 定义趋势区域的阈值。
  - `BuyOpen`、`SellOpen`、`BuyClose`、`SellClose` — 各方向操作的开关。
