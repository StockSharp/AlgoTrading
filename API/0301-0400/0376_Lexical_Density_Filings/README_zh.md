# Lexical Density Filings

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

读取各证券最新已发布的纯文本报告。词元为 Unicode 字母及组合标记序列，以 .NET 不变文化规则统一大小写。词汇密度 = 不同规范化词元数量 / 全部词元数量。仅在二、五、八、十一月的前三个交易日期内分配，日历取第一个证券的交易所。在窗口内等待至少 Quintile 份有效报告，否则等到下个审核月。做多密度最高组、做空最低组，每侧 floor(有效数量 / Quintile) 个，期间保持仓位。

所有观测必须在信号蜡烛收盘前发布。日内执行仅使用当前 UTC 日期之前的日价格；完整日蜡烛可计入自身日期。未来 CSV 日期不得提前预热决策。

发布截止时刻为同步完成蜡烛中最晚的实际 CloseTime。交易时段可能早于所设完整蜡烛间隔结束。

## 输入数据

FilingsFile 为必需 CSV：`SecurityId,PeriodEnd,PublishedAt,DocumentPath`。日期格式为 `yyyy-MM-dd` 和 UTC `yyyy-MM-ddTHH:mm:ssZ`。DocumentPath 指向 UTF-8 纯文本，可为绝对路径或相对清单目录的路径，不得含逗号。拒绝空文档、重复修订及早于 PeriodEnd 的发布。只选收盘时已发布的最新期间/修订，限制 MaxReportAgeDays。首个证券须提供含假日的交易所日历。

词边界按首类别为字母或组合标记的 Unicode 文本元素确定，标点和数字分隔词元。仅使用不变大小写规范，不做词干化或重音规范化。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `Quintile` | `5` |
| `MaxReportAgeDays` | `550` |
| `FilingsFile` | 必填 |

## 成交与仓位

必须显式设置 Universe。价格与 Capital 采用 USD，每个价格单位对应一个交易单位。Capital 是固定预算，Leverage 控制总敞口。不添加止损或止盈。

多头和空头各分配 Capital × Leverage 的一半，各篮子内等额分配。按信号收盘价换算数量，并按 VolumeStep 向零取整。根据实际成交仓位调整，等待未完成订单，遵守 MinVolume/MaxVolume，并在后续蜡烛继续大额调整。MinTradeUsd 过滤小额调整，但完整平仓不受此过滤限制。

## 验证

两种语言均使用随附 BTC/TON 执行数据和明确的利率、财务、评分、文本及预热测试数据。日历测试重新定位归档 OHLC/成交量并提供确定性报价，并非多年市场历史。检查独立公式、订单方向/数量、真实成交、剩余仓位及禁止决策日期。两组、K=1 和 AssetDecile=1 是明确的机制测试配置，不能证明完整生产证券池。无盈利承诺。
