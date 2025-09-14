# 随机指标柱状策略

该策略是 MQL 专家 `Exp_Stochastic_Histogram` 的 StockSharp 移植版本。
它使用随机指标在两种模式下生成逆势交易信号：

- **Levels** – 当 %K 离开由 `HighLevel` 和 `LowLevel` 定义的超买或超卖区域时产生信号。
- **Cross** – 当 %K 与 %D 线交叉时产生信号，交易方向与交叉方向相反。

收到新信号时，策略先关闭已有仓位，然后按照信号方向开立新仓位。

## 参数

- `KPeriod` – %K 主周期。
- `DPeriod` – %D 平滑周期。
- `Slowing` – %K 额外平滑值。
- `HighLevel` – Levels 模式的上阈值。
- `LowLevel` – Levels 模式的下阈值。
- `Mode` – Levels 或 Cross。
- `CandleType` – 计算所用 K 线周期。

## 工作原理

每当一根 K 线收盘时，随机指标会被更新并进行评估。 在 **Levels** 模式下，%K 回落到上阈值以下时做多，%K 上升到下阈值以上时做空。 在 **Cross** 模式下，%K 向下穿越 %D 触发多单，向上穿越触发空单。策略始终保证市场中只有一个方向的仓位。
