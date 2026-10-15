# 颜色代码叠加策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

根据自定义颜色代码计算的蜡烛颜色变化进行交易，并使用固定点数的止损和止盈。

## 逻辑
- 通过 OHLC 构建自定义颜色蜡烛。
- 当蜡烛实体超过范围的 1% 时检测颜色切换。
- 按交易类型在由红转绿时做多，由绿转红时做空。
- 仅在 `StartTime` 和 `EndTime` 之间运行。
- 应用 `StopLossPips` 和 `TakeProfitPips` 保护。
