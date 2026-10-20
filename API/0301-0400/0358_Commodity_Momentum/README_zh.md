# Commodity Momentum

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

M 为决策月，P[K] 为完整日历月 K 中最后观测的日收盘。Momentum = P[M − SkipMonths − 1] / P[M − LookbackMonths − 1] − 1。端点间所有月份必须存在。默认三月调仓时比较本年一月与上年二月，排除二月和未完成三月。最多买入排名最高的 TopN 个标的，等额分配 Capital × Leverage，只做多。负动量仍可入选，不设绝对收益过滤。下月调仓关闭移除标的。

仅处理时间戳一致的已完成蜡烛。统计按不同 UTC 日期计数，同一天的盘中收盘值会被后续值替换。每月在第一个满足条件的同步收盘信号上再平衡一次；数据不足时延迟。相同分数按 Security.Id 排序。

## 输入数据

PriceHistoryFile 为可选的正值日收盘价 CSV：SecurityId,Date,Close；日期格式 yyyy-MM-dd。当前或未来日期不能提前预热决策，实时已完成蜡烛覆盖对应 UTC 日期。禁止重复日期。执行历史较短时可用于长周期预热。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `PriceHistoryFile` | 可选 — 未提供 |
| `TopN` | `5` |
| `LookbackMonths` | `12` |
| `SkipMonths` | `1` |

## 成交与仓位

必须显式设置 Universe。价格与 Capital 采用 USD，每个价格单位对应一个交易单位。Capital 是固定预算，Leverage 控制总敞口。不添加止损或止盈。

按信号收盘价换算数量，并按 VolumeStep 向零取整。根据实际成交仓位调整，等待未完成订单，遵守 MinVolume/MaxVolume，并在后续蜡烛继续大额调整。MinTradeUsd 过滤小额调整，但完整平仓不受此过滤限制。

## 验证

成交测试仅使用已有 BTC/TON 执行档案。显式财报、期权观测和预热数据是机制测试夹具，不是真实企业财务或期权历史。缩短周期、两组排序和三月年度调仓是明确的测试配置，不代表原始长期默认值已完整交易验证。不声称任何收益率。
