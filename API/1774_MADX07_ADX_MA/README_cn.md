# MADX-07 ADX MA 策略

该策略由 MQL4 平台的 MADX-07 EA 转换而来，使用 H4 K 线，将两条移动平均线与平均趋向指数 (ADX) 结合作为过滤器。

## 逻辑

- 做多：价格高于慢速 MA，快速 MA 高于慢速 MA，最近两根 K 线的价格至少比快速 MA 高 `MaDifference` 点，ADX 上升并高于 `AdxMainLevel`，+DI 上升，-DI 下降。
- 做空：条件相反。
- 当盈利达到 `CloseProfit` 点或挂单在 `TakeProfit` 距离被执行时，仓位将平仓。

## 参数

- `BigMaPeriod` (25) – 慢速 MA 的周期。
- `BigMaType` – 慢速 MA 的类型。
- `SmallMaPeriod` (5) – 快速 MA 的周期。
- `SmallMaType` – 快速 MA 的类型。
- `MaDifference` (5) – 价格与快速 MA 之间的最小距离（点）。
- `AdxPeriod` (11) – ADX 计算周期。
- `AdxMainLevel` (13) – ADX 的最小值。
- `AdxPlusLevel` (13) – +DI 的最小值。
- `AdxMinusLevel` (14) – -DI 的最小值。
- `TakeProfit` (299) – 止盈距离（点）。
- `CloseProfit` (13) – 提前平仓的利润点数。
- `Volume` (0.1) – 交易量。
- `CandleType` – K 线时间框（默认 H4）。
