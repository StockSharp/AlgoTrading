# Bearish Wick Reversal

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

本策略只做多，使用已完成的一分钟阴线下影线反转。空仓时，若 `Close < Open` 且 `100 * (Low - Close) / Close <= Threshold`，买入 `Volume`。默认 `-1` 表示下影线至少为收盘价的百分之一。

开仓要求蜡烛 UTC 开盘时间位于包含端点的 `StartTime`–`EndTime` 区间。启用 `UseEmaFilter` 时，还须 `EMA(EmaPeriod)` 已形成并且 `Close > EMA`；关闭过滤器时，EMA 预热不延迟入场。

当完整蜡烛的收盘价高于上一根完整蜡烛的最高价时，平掉全部实际多仓。开仓时间窗口和 EMA 不阻止退出。不做空，没有隐藏的等待根数，也没有自动止盈止损。活动订单防止重复提交。

## 参数

| 参数 | 默认值 |
|---|---|
| `Threshold` | `-1` |
| `UseEmaFilter` | `false` |
| `EmaPeriod` | `200` |
| `CandleType` | `1m` |
| `StartTime` | `2014-01-01T00:00:00Z` |
| `EndTime` | `2099-01-01T00:00:00Z` |
