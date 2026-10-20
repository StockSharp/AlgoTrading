# Consistent Momentum

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

长动量 P[d] / P[d − LookbackDays] − 1，短动量 P[d] / P[d − ShortLookbackDays] − 1，使用不同日历日期，至少 LookbackDays + 1 个收盘。独立排序两项：买两者最高 Deciles 组的交集，卖空最低组交集；每个极端组 floor(Universe 数 / Deciles) 个候选。每月建立一批，预算 Capital × Leverage / HoldingMonths，两侧各半；空侧留现金。创建时固定取整数量与到期月（创建月加 HoldingMonths）。合计未到期批次数量，只交易差额。之后修改 HoldingMonths 不改变旧批次到期日。

仅处理时间戳一致的已完成蜡烛。统计按不同 UTC 日期计数，同一天的盘中收盘值会被后续值替换。每月在第一个满足条件的同步收盘信号上再平衡一次；数据不足时延迟。相同分数按 Security.Id 排序。

盘中 CandleType 的日统计只使用严格早于当前信号日期的 UTC 日期，排除尚未完成的当天。已完成的一日蜡烛可计入自己的完整日期。

## 输入数据

PriceHistoryFile 为可选的正值日收盘价 CSV：SecurityId,Date,Close；日期格式 yyyy-MM-dd。当前或未来日期不能提前预热决策，实时已完成蜡烛覆盖对应 UTC 日期。禁止重复日期。执行历史较短时可用于长周期预热。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `50` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `PriceHistoryFile` | 可选 — 未提供 |
| `LookbackDays` | `147` |
| `ShortLookbackDays` | `21` |
| `HoldingMonths` | `6` |

## 成交与仓位

必须显式设置 Universe。价格与 Capital 采用 USD，每个价格单位对应一个交易单位。Capital 是固定预算，Leverage 控制总敞口。不添加止损或止盈。

按信号收盘价换算数量，并按 VolumeStep 向零取整。根据实际成交仓位调整，等待未完成订单，遵守 MinVolume/MaxVolume，并在后续蜡烛继续大额调整。MinTradeUsd 过滤小额调整，但完整平仓不受此过滤限制。

## 验证

成交测试仅使用已有 BTC/TON 执行档案。显式财报、期权观测和预热数据是机制测试夹具，不是真实企业财务或期权历史。缩短周期、两组排序和三月年度调仓是明确的测试配置，不代表原始长期默认值已完整交易验证。不声称任何收益率。
