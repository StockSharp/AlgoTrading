# F-Score Reversal

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

每月计算 Lookback 日收益 P[d] / P[d−Lookback] − 1。在已发布 FScore >= FHi 的证券中买入收益最低者；若 IsShortEnabled，在 FScore <= FLo 中做空收益最高者。阈值包含边界，FHi 必须大于 FLo。输入为真实发布的 Piotroski 整数评分 0..9，采用最新已知期间及修订，并限制 MaxReportAgeDays。每侧占总预算一半，缺失或禁用侧保留现金。先全部平掉旧组合并等待成交，再开立新月仓位。

仅处理时间戳一致的已完成蜡烛。统计按不同 UTC 日期计数，同一天的盘中收盘值会被后续值替换。每月在第一个满足条件的同步收盘信号上再平衡一次；数据不足时延迟。相同分数按 Security.Id 排序。

所有观测必须在信号蜡烛收盘前发布。日内执行仅使用当前 UTC 日期之前的日价格；完整日蜡烛可计入自身日期。未来 CSV 日期不得提前预热决策。

发布截止时刻为同步完成蜡烛中最晚的实际 CloseTime。交易时段可能早于所设完整蜡烛间隔结束。

## 输入数据

PriceHistoryFile 为可选的正值日收盘价 CSV：SecurityId,Date,Close；日期格式 yyyy-MM-dd。当前或未来日期不能提前预热决策，实时已完成蜡烛覆盖对应 UTC 日期。禁止重复日期。执行历史较短时可用于长周期预热。

FScoreFile 为必需 CSV：`SecurityId,PeriodEnd,PublishedAt,FScore`。PeriodEnd 用 `yyyy-MM-dd`，PublishedAt 用 UTC `yyyy-MM-ddTHH:mm:ssZ`。只接受整数 0..9、唯一修订及不早于 PeriodEnd 的发布。选择最新已知期间及修订，年龄 MaxReportAgeDays 自 PeriodEnd 起算。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | 可选 — 未提供 |
| `Lookback` | `21` |
| `FHi` | `7` |
| `FLo` | `3` |
| `IsShortEnabled` | `true` |
| `MaxReportAgeDays` | `550` |
| `FScoreFile` | 必填 |

## 成交与仓位

必须显式设置 Universe。价格与 Capital 采用 USD，每个价格单位对应一个交易单位。Capital 是固定预算，Leverage 控制总敞口。不添加止损或止盈。

目标数量按 VolumeStep 向零取整。新开仓前等待全部平仓实际成交，依据真实仓位协调，遵守 MinVolume/MaxVolume，大额调整分散到后续蜡烛。MinTradeUsd 过滤非零目标调整，完整平仓绕过此限制。

## 验证

两种语言均使用随附 BTC/TON 执行数据和明确的利率、财务、评分、文本及预热测试数据。日历测试重新定位归档 OHLC/成交量并提供确定性报价，并非多年市场历史。检查独立公式、订单方向/数量、真实成交、剩余仓位及禁止决策日期。两组、K=1 和 AssetDecile=1 是明确的机制测试配置，不能证明完整生产证券池。无盈利承诺。
