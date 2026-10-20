# Asset Growth Effect

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

资产增长率 = 最新已公布财年 TotalAssets / 一年前同一财政日期 TotalAssets − 1。两个报告都必须在信号前公布。买最低 Quantiles 组，卖空最高组，每侧 floor(合格数 / Quantiles) 个标的。至少需要 Quantiles 对报告。默认每年七月再平衡。

每年仅在 RebalanceMonth 的第一个合格同步收盘信号再平衡一次。后来公布的报告不能影响较早决策。MaxReportAgeDays 从 PeriodEnd 起计算。

## 输入数据

FundamentalsFile 为必填 CSV：SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding。PeriodEnd 为 yyyy-MM-dd，PublishedAt 为 yyyy-MM-ddTHH:mm:ssZ（UTC）。资产和股数必须为正。只选信号时已公布的最新财年及其最新修订。拒绝重复修订和期末之前的公布时间。禁止从蜡烛价格伪造财务数据。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `50` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Quantiles` | `10` |
| `FundamentalsFile` | 必填 |
| `RebalanceMonth` | `7` |
| `MaxReportAgeDays` | `550` |

## 成交与仓位

必须显式设置 Universe。价格与 Capital 采用 USD，每个价格单位对应一个交易单位。Capital 是固定预算，Leverage 控制总敞口。不添加止损或止盈。

多头和空头各分配 Capital × Leverage 的一半，各篮子内等额分配。按信号收盘价换算数量，并按 VolumeStep 向零取整。根据实际成交仓位调整，等待未完成订单，遵守 MinVolume/MaxVolume，并在后续蜡烛继续大额调整。MinTradeUsd 过滤小额调整，但完整平仓不受此过滤限制。

## 验证

成交测试仅使用已有 BTC/TON 执行档案。显式财报、期权观测和预热数据是机制测试夹具，不是真实企业财务或期权历史。缩短周期、两组排序和三月年度调仓是明确的测试配置，不代表原始长期默认值已完整交易验证。不声称任何收益率。
