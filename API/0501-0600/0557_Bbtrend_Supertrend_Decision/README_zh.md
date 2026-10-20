# BBTrend SuperTrend Decision

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

在已完成的收盘价上计算两组布林带：20 与 50 根，宽度为两倍总体标准差。短/长下轨为 `Ls/Ll`、上轨为 `Us/Ul`、短期均值为 `Ms` 时，`BBTrend = 100 * (abs(Ls-Ll) - abs(Us-Ul)) / Ms`。

对 BBTrend 应用 SuperTrend。前值与当前值分别作为合成开盘和收盘，两者的最小/最大值作为低/高。真实波幅为分数的绝对变化；ATR 先取十个波幅的平均，再用 Wilder 平滑。轨道为合成中点加减 `7 * ATR`，未突破时沿用跟踪边界。两组布林带与 ATR 全部形成后才能交易。

上升趋势目标为 `+Volume`，下降趋势为 `-Volume`，按目标与当前仓位的差额下单以完整反转。`TradeDirection = Buy/Sell` 限定开仓方向，反向信号仍平掉已有仓位；`null` 允许双向。活动订单期间不重复提交。

`TpSlCondition` 可选 `None`、`TP`、`SL`、`Both`（Python 为 0/1/2/3），默认关闭。启用的止盈和止损分别使用 `TakeProfitPerc`、`StopLossPerc` 百分比，而非绝对价差。

本地原生保护使用该品种的蜡烛和已订阅价格更新，距离以实际入场成交为基准。退出为市价订单，成交价受可用市场及价格步长影响。

## 参数

| 参数 | 默认值 |
|---|---|
| `ShortBbLength` | `20` |
| `LongBbLength` | `50` |
| `StdDev` | `2` |
| `SupertrendLength` | `10` |
| `SupertrendMultiplier` | `7` |
| `TradeDirection` | `null` |
| `TpSlCondition` | `None` |
| `TakeProfitPerc` | `30` |
| `StopLossPerc` | `20` |
| `CandleType` | `5m` |
