# Momentum Factor Stocks

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

每次月度审核需要 LookbackDays + 1 个完整日收盘价。Momentum = P[d−SkipDays] / P[d−LookbackDays] − 1，要求 SkipDays < LookbackDays。明确排除最新 SkipDays 个日收益区间，不以两个 ROC 指标相减代替。做多最高 Quintile 组、做空最低组，每侧 floor(有效数量 / Quintile) 个。保留取整后的原生数量至下次月度审核。

仅处理时间戳一致的已完成蜡烛。统计按不同 UTC 日期计数，同一天的盘中收盘值会被后续值替换。每月在第一个满足条件的同步收盘信号上再平衡一次；数据不足时延迟。相同分数按 Security.Id 排序。

所有观测必须在信号蜡烛收盘前发布。日内执行仅使用当前 UTC 日期之前的日价格；完整日蜡烛可计入自身日期。未来 CSV 日期不得提前预热决策。

## 输入数据

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
| `LookbackDays` | `252` |
| `SkipDays` | `21` |
| `Quintile` | `5` |

## 成交与仓位

必须显式设置 Universe。价格与 Capital 采用 USD，每个价格单位对应一个交易单位。Capital 是固定预算，Leverage 控制总敞口。不添加止损或止盈。

多头和空头各分配 Capital × Leverage 的一半，各篮子内等额分配。按信号收盘价换算数量，并按 VolumeStep 向零取整。根据实际成交仓位调整，等待未完成订单，遵守 MinVolume/MaxVolume，并在后续蜡烛继续大额调整。MinTradeUsd 过滤小额调整，但完整平仓不受此过滤限制。

## 验证

两种语言均使用随附 BTC/TON 执行数据和明确的利率、财务、评分、文本及预热测试数据。日历测试重新定位归档 OHLC/成交量并提供确定性报价，并非多年市场历史。检查独立公式、订单方向/数量、真实成交、剩余仓位及禁止决策日期。两组、K=1 和 AssetDecile=1 是明确的机制测试配置，不能证明完整生产证券池。无盈利承诺。
