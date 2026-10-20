# BTC Difficulty Adjustments

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

必须提供带 `Time,Difficulty` 表头的 `DifficultyHistoryFile`。发布时间必须是严格递增且以 `Z` 结尾的 ISO UTC 时间，难度必须为正。启动时读取文件；缺失或格式错误会明确报错。不会从市场价格推导挖矿难度。

每根已完成蜡烛仅使用发布时间不晚于其 `OpenTime` 的观测。首条记录建立基准，难度不变时不产生信号。变化为 `100*(difficulty/previous-1)`。`ThresholdMode=false` 时上涨买入、下降卖出。阈值模式仅在 `abs(change)>=Threshold` 时行动：下降买入、上涨卖出。最新满足条件的已知变化决定方向。空仓开 `Volume`，反转下单 `Volume+abs(Position)`。未完成订单阻止重叠决策，无自动止损。

## 参数

| 参数 | 默认值 |
|---|---|
| `CandleType` | `1d` |
| `ThresholdMode` | `false` |
| `Threshold` | `10` |
| `DifficultyHistoryFile` | `必填` |
