# FX Carry Trade

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

每月 carry = 实测年 BaseRate − QuoteRate。做多最高 TopK 个差值、做空最低 TopK 个；至少需要 2×TopK 个有效货币对以避免重叠。两侧各占美元总预算的一半，侧内等额分配。使用明确观测的 UsdPerUnit 把美元敞口换为原生交易单位，支持报价货币不是美元的货币对。下次审核时平掉离组证券并调整继续持有的仓位。

仅处理时间戳一致的已完成蜡烛。统计按不同 UTC 日期计数，同一天的盘中收盘值会被后续值替换。每月在第一个满足条件的同步收盘信号上再平衡一次；数据不足时延迟。相同分数按 Security.Id 排序。

所有观测必须在信号蜡烛收盘前发布。日内执行仅使用当前 UTC 日期之前的日价格；完整日蜡烛可计入自身日期。未来 CSV 日期不得提前预热决策。

发布截止时刻为同步完成蜡烛中最晚的实际 CloseTime。交易时段可能早于所设完整蜡烛间隔结束。

## 输入数据

RatesFile 为必需 CSV：`SecurityId,PublishedAt,BaseRate,QuoteRate,UsdPerUnit`。PublishedAt 为 UTC `yyyy-MM-ddTHH:mm:ssZ`，利率使用相同的年化小数单位。UsdPerUnit 是每个原生交易单位的正美元价值。只采用最新发布且年龄不超过 MaxRateAgeDays 的记录，拒绝证券/发布时间重复项。禁止用蜡烛推算利率。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `TopK` | `3` |
| `RatesFile` | 必填 |
| `MaxRateAgeDays` | `45` |

## 成交与仓位

Capital 为固定美元预算，Leverage 控制总敞口。数量及 MinTradeUsd 采用 RatesFile.UsdPerUnit。按 VolumeStep 向零取整，依据真实成交协调并等待未完成订单，遵守 MinVolume/MaxVolume，大额调整分散到后续蜡烛。完整平仓不受 MinTradeUsd 限制。无止损或止盈。

## 验证

两种语言均使用随附 BTC/TON 执行数据和明确的利率、财务、评分、文本及预热测试数据。日历测试重新定位归档 OHLC/成交量并提供确定性报价，并非多年市场历史。检查独立公式、订单方向/数量、真实成交、剩余仓位及禁止决策日期。两组、K=1 和 AssetDecile=1 是明确的机制测试配置，不能证明完整生产证券池。无盈利承诺。
