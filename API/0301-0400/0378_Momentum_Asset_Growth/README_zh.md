# Momentum Asset Growth

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

用最新已发布年度报告和上一年相同财务日期计算资产增长 = TotalAssets[current] / TotalAssets[previous] − 1。仅保留增长最高的 AssetDecile 组，即 floor(财务有效数量 / AssetDecile) 个。在该子集中，用截至决策月月初向前 SkipMonths 个完整月之前的 MomLook + 1 个日收盘价计算 MomLook 日动量。做多最高 Quintile 组、做空最低组；筛选后至少需要 Quintile 个预热完整证券。每月审核，一月除外。一月保留原仓，不重新分配也不强制平仓。

仅处理时间戳一致的已完成蜡烛。统计按不同 UTC 日期计数，同一天的盘中收盘值会被后续值替换。每月在第一个满足条件的同步收盘信号上再平衡一次；数据不足时延迟。相同分数按 Security.Id 排序。

所有观测必须在信号蜡烛收盘前发布。日内执行仅使用当前 UTC 日期之前的日价格；完整日蜡烛可计入自身日期。未来 CSV 日期不得提前预热决策。

发布截止时刻为同步完成蜡烛中最晚的实际 CloseTime。交易时段可能早于所设完整蜡烛间隔结束。

## 输入数据

FundamentalsFile 为必填 CSV：SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding。PeriodEnd 为 yyyy-MM-dd，PublishedAt 为 yyyy-MM-ddTHH:mm:ssZ（UTC）。资产和股数必须为正。只选信号时已公布的最新财年及其最新修订。拒绝重复修订和期末之前的公布时间。禁止从蜡烛价格伪造财务数据。

PriceHistoryFile 为可选的正值日收盘价 CSV：SecurityId,Date,Close；日期格式 yyyy-MM-dd。当前或未来日期不能提前预热决策，实时已完成蜡烛覆盖对应 UTC 日期。禁止重复日期。执行历史较短时可用于长周期预热。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | 可选 — 未提供 |
| `MomLook` | `252` |
| `SkipMonths` | `1` |
| `AssetDecile` | `10` |
| `Quintile` | `5` |
| `MaxReportAgeDays` | `550` |
| `FundamentalsFile` | 必填 |

## 成交与仓位

必须显式设置 Universe。价格与 Capital 采用 USD，每个价格单位对应一个交易单位。Capital 是固定预算，Leverage 控制总敞口。不添加止损或止盈。

多头和空头各分配 Capital × Leverage 的一半，各篮子内等额分配。按信号收盘价换算数量，并按 VolumeStep 向零取整。根据实际成交仓位调整，等待未完成订单，遵守 MinVolume/MaxVolume，并在后续蜡烛继续大额调整。MinTradeUsd 过滤小额调整，但完整平仓不受此过滤限制。

## 验证

两种语言均使用随附 BTC/TON 执行数据和明确的利率、财务、评分、文本及预热测试数据。日历测试重新定位归档 OHLC/成交量并提供确定性报价，并非多年市场历史。检查独立公式、订单方向/数量、真实成交、剩余仓位及禁止决策日期。两组、K=1 和 AssetDecile=1 是明确的机制测试配置，不能证明完整生产证券池。无盈利承诺。
