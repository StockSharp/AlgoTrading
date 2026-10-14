# Precision Trading: Golden Edge
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA 交叉策略，由 Hull 方向、RSI 动量和 ATR/高低价区间确认。历史描述面向黄金剥头皮；代码使用所配置的证券，测试不证明黄金交易表现。

## 信号与执行

只使用已完成的蜡烛，默认五分钟。七个指标必须全部形成；第一根完全预热的蜡烛仅记录前一 EMA/HMA，不入场。

- 多头：前一快 EMA ≤ 前一慢 EMA，当前快 EMA > 当前慢 EMA，当前 HMA > 前一 HMA，RSI > `RsiLongThreshold`（55）。
- 空头：前一快 EMA ≥ 前一慢 EMA，当前快 EMA < 当前慢 EMA，当前 HMA < 前一 HMA，RSI < `RsiShortThreshold`（45）。
- 两个方向都要求 close 和 ATR 为正，`100 * ATR / close >= MinAtrPercent`（0.05），以及 `Highest(high) - Lowest(low) >= ATR * RangeAtrMultiplier`（3）。极值窗口包含当前蜡烛。

满足条件时，从空仓买卖 `Volume`，或用 `Volume + abs(Position)` 完全反转相反持仓。同方向信号保持持仓。未完成订单阻止重复提交。每根已预热蜡烛都更新前一 EMA/HMA，被拒绝的交叉不会延迟入场。没有六小时冷却，也没有额外止损或止盈；相反方向的合格交叉反转持仓。

## 指标与默认参数

| 参数 | 默认值 | 含义 |
|---|---:|---|
| `FastLength` / `SlowLength` | 14 / 40 | 原生收盘价 EMA；快周期 < 慢周期 |
| `HmaLength` | 55 | `WMA(floor(sqrt(L)), 2 * WMA(floor(L/2), close) - WMA(L, close))`；L ≥ 2 |
| `RsiLength` | 14 | 原生 RSI，Wilder 平滑收盘涨跌 |
| `RsiLongThreshold` / `RsiShortThreshold` | 55 / 45 | 严格阈值；`0 <= short < long <= 100` |
| `AtrLength` | 14 | 原生 Wilder ATR，计入相对前一 close 的跳空 |
| `RangeLength` | 20 | 包含当前蜡烛的最高 high 和最低 low |
| `MinAtrPercent` | 0.05 | ATR 占当前 close 的百分比下限，包含边界，非负 |
| `RangeAtrMultiplier` | 3 | 高低区间的 ATR 倍数下限，包含边界，正数 |
| `CandleType` | 5 分钟 | 全部指标的已完成蜡烛 |

WMA 权重从最旧的 1 到最新的窗口长度。EMA 以原生算术均值初始化，之后使用 `2 / (length + 1)`。ATR 从增长窗口均值开始，随后使用 Wilder 平滑。周期为正；HMA 还需完成最后的平方根周期 WMA。

C# 与 Python 测试在现有 BTC/TON 历史上独立计算 EMA、HMA、RSI、ATR 和滚动极值，验证每个订单、完全反转、被拒绝的确认及小于六小时的信号。测试验证行为，不验证盈利或实盘适用性。
