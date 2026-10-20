# ESG Factor Momentum

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

Universe 必须由用户预先完成 ESG 筛选，策略不生成 ESG 评分。每月按 LookbackDays + 1 个完整日收盘价计算 momentum = P[d] / P[d−LookbackDays] − 1。仅买入最高收益者，即使所有收益为负。每次月度审核都先完全平掉旧仓并等待真实成交，然后买入获胜者，即使获胜者未变。使用不超过已报告组合价值的全部预算；无杠杆、无空头。

仅处理时间戳一致的已完成蜡烛。统计按不同 UTC 日期计数，同一天的盘中收盘值会被后续值替换。每月在第一个满足条件的同步收盘信号上再平衡一次；数据不足时延迟。相同分数按 Security.Id 排序。

所有观测必须在信号蜡烛收盘前发布。日内执行仅使用当前 UTC 日期之前的日价格；完整日蜡烛可计入自身日期。未来 CSV 日期不得提前预热决策。

## 输入数据

PriceHistoryFile 为可选的正值日收盘价 CSV：SecurityId,Date,Close；日期格式 yyyy-MM-dd。当前或未来日期不能提前预热决策，实时已完成蜡烛覆盖对应 UTC 日期。禁止重复日期。执行历史较短时可用于长周期预热。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | `100000` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | 可选 — 未提供 |
| `LookbackDays` | `252` |

## 成交与仓位

明确指定 Universe，执行价格和 Capital 单位为每交易单位美元。多头敞口上限为 min(Capital, 已报告 Portfolio.CurrentValue)，未知或非正组合价值不给出买入预算。无杠杆、止损或止盈。

目标数量按 VolumeStep 向零取整。新开仓前等待全部平仓实际成交，依据真实仓位协调，遵守 MinVolume/MaxVolume，大额调整分散到后续蜡烛。MinTradeUsd 过滤非零目标调整，完整平仓绕过此限制。

## 验证

两种语言均使用随附 BTC/TON 执行数据和明确的利率、财务、评分、文本及预热测试数据。日历测试重新定位归档 OHLC/成交量并提供确定性报价，并非多年市场历史。检查独立公式、订单方向/数量、真实成交、剩余仓位及禁止决策日期。两组、K=1 和 AssetDecile=1 是明确的机制测试配置，不能证明完整生产证券池。无盈利承诺。
