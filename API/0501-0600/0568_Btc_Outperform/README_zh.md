# BTC Outperform

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

将已完成的 UTC 日线聚合为固定周期。周周期锚定星期一 1970-01-05，90 天周期锚定 1970-01-01。只有从起点到终点的所有日线均存在时才发布该周期收盘价。启动时的不完整周期以及存在缺口的周期不发布收盘价。

比较最近完整观测且已完成的收盘价：周收盘较高则买入，较低则卖出，相等则保持原仓位。空仓下单 `Volume`，反转下单 `Volume+abs(Position)`，留下完整反向仓位。未完成周期不影响比较。未完成订单防止重复提交，不设置自动价格止损或止盈。

## 参数

| 参数 | 默认值 |
|---|---|
| `CandleType` | `1d` |
| `WeeklyDays` | `7` |
| `QuarterlyDays` | `90` |
