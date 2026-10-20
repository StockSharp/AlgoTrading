# Earnings Quality Factor

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

盈利质量明确为 (OperatingCashFlow − NetIncome) / TotalAssets。在 RebalanceMonth 首根有效同步完成蜡烛处，选择截至该蜡烛收盘已发布的最新财年及其最新修订。做多质量最高的 Deciles 组，做空最低组，每侧 floor(有效数量 / Deciles) 个。每年仅调整一次，持有至下一次审核。自 PeriodEnd 起的报告年龄不得超过 MaxReportAgeDays。

每年仅在 RebalanceMonth 的第一个合格同步收盘信号再平衡一次。后来公布的报告不能影响较早决策。MaxReportAgeDays 从 PeriodEnd 起计算。

所有观测必须在信号蜡烛收盘前发布。日内执行仅使用当前 UTC 日期之前的日价格；完整日蜡烛可计入自身日期。未来 CSV 日期不得提前预热决策。

发布截止时刻为同步完成蜡烛中最晚的实际 CloseTime。交易时段可能早于所设完整蜡烛间隔结束。

## 输入数据

FundamentalsFile 为必填 CSV：SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding。PeriodEnd 为 yyyy-MM-dd，PublishedAt 为 yyyy-MM-ddTHH:mm:ssZ（UTC）。资产和股数必须为正。只选信号时已公布的最新财年及其最新修订。拒绝重复修订和期末之前的公布时间。禁止从蜡烛价格伪造财务数据。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `RebalanceMonth` | `7` |
| `MaxReportAgeDays` | `550` |
| `FundamentalsFile` | 必填 |

## 成交与仓位

必须显式设置 Universe。价格与 Capital 采用 USD，每个价格单位对应一个交易单位。Capital 是固定预算，Leverage 控制总敞口。不添加止损或止盈。

多头和空头各分配 Capital × Leverage 的一半，各篮子内等额分配。按信号收盘价换算数量，并按 VolumeStep 向零取整。根据实际成交仓位调整，等待未完成订单，遵守 MinVolume/MaxVolume，并在后续蜡烛继续大额调整。MinTradeUsd 过滤小额调整，但完整平仓不受此过滤限制。

## 验证

两种语言均使用随附 BTC/TON 执行数据和明确的利率、财务、评分、文本及预热测试数据。日历测试重新定位归档 OHLC/成交量并提供确定性报价，并非多年市场历史。检查独立公式、订单方向/数量、真实成交、剩余仓位及禁止决策日期。两组、K=1 和 AssetDecile=1 是明确的机制测试配置，不能证明完整生产证券池。无盈利承诺。
