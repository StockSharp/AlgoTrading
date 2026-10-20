# Fib Hurst Breakout

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

CandleType 为日内周期。斐波那契区间取信号日之前 LookbackPeriod 根完整 UTC 日蜡烛最高高点和最低低点，水平为 L+0.618×(H−L)、L+0.382×(H−L)。日 Hurst 用此前 HurstPeriod 个收盘价：H = log(R/S)/log(N)，R 为相对窗口均值的累计偏差范围，S 为总体标准差；零方差阻止入场。前后两个日内收盘与同一当前日水平比较：上穿 61.8% 且 H>0.5 做多，下穿 38.2% 且 H<0.5 做空。只从空仓以 Volume 入场。MaxTradesPerDay 按历史 UTC 日期统计入场订单尝试，MaxTotalTrades 按本次运行统计。原生报价保护以真实成交固定 SL = RiskPercent%、TP = RiskPercent×RiskReward%。无反向信号翻仓或冷却。

## 参数

| 参数 | 默认值 |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(15).TimeFrame() |
| `HurstPeriod` | 50 |
| `LookbackPeriod` | 20 |
| `MaxTradesPerDay` | 5 |
| `MaxTotalTrades` | 510 |
| `RiskPercent` | 2 |
| `RiskReward` | 2 |
| `DailyHistoryFile` | 未提供 |

## 输入数据

可选 `DailyHistoryFile` CSV：`Date,High,Low,Close`，日期 `yyyy-MM-dd`，日期唯一且 0 < Low ≤ Close ≤ High。只采用日内信号日之前的日期。完整日蜡烛替换对应日，未来行不影响之前决策。

## 验证

两种语言使用随附 BTC/TON，检查独立公式、订单方向/数量、真实成交、仓位与退出。带日期的价格、因子、日历数据验证机制，重定位 OHLC/成交量不是多年历史。两个排名组、TopN=1、SMA=2 是明确的小证券池测试配置，长窗口和原始默认值另行检查。不承诺盈利。
