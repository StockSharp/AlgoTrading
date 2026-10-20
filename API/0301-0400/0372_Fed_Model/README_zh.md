# Fed Model

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

Universe 顺序为 [股票 ETF，可选现金 ETF]。仅使用日蜡烛和第一个证券的交易所日历。在每月最后一个交易日收盘，要求此前已发布的 RegressionMonths 个连续月度观测。带截距回归：y = EarningsYield − BondYield，x = 0..N−1；slope = Σ((x−mean(x))×(y−mean(y))) / Σ(x−mean(x))²；预测 = mean(y) + slope×(N−mean(x))。正值持有股票，负值持有现金 ETF 或现金，零值保持原状态。仅在状态改变时交易，同一状态不按月重设仓位。必须先完成旧仓平仓成交再买入。

所有观测必须在信号蜡烛收盘前发布。日内执行仅使用当前 UTC 日期之前的日价格；完整日蜡烛可计入自身日期。未来 CSV 日期不得提前预热决策。

发布截止时刻为同步完成蜡烛中最晚的实际 CloseTime。交易时段可能早于所设完整蜡烛间隔结束。

## 输入数据

YieldHistoryFile 为必需 CSV：`Month,PublishedAt,EarningsYield,BondYield`。Month 用 `yyyy-MM`，PublishedAt 用 UTC `yyyy-MM-ddTHH:mm:ssZ`。两项收益率单位相同。仅采用截至决策收盘已发布的修订。需要连续月份并包含决策月，缺失则延迟该月预测。拒绝重复修订及早于观测月份的发布。使用实测市场/财务数据，不用反价格代理。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | `100000` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `RegressionMonths` | `12` |
| `YieldHistoryFile` | 必填 |

## 成交与仓位

明确指定 Universe，执行价格和 Capital 单位为每交易单位美元。多头敞口上限为 min(Capital, 已报告 Portfolio.CurrentValue)，未知或非正组合价值不给出买入预算。无杠杆、止损或止盈。

目标数量按 VolumeStep 向零取整。新开仓前等待全部平仓实际成交，依据真实仓位协调，遵守 MinVolume/MaxVolume，大额调整分散到后续蜡烛。MinTradeUsd 过滤非零目标调整，完整平仓绕过此限制。

## 验证

两种语言均使用随附 BTC/TON 执行数据和明确的利率、财务、评分、文本及预热测试数据。日历测试重新定位归档 OHLC/成交量并提供确定性报价，并非多年市场历史。检查独立公式、订单方向/数量、真实成交、剩余仓位及禁止决策日期。两组、K=1 和 AssetDecile=1 是明确的机制测试配置，不能证明完整生产证券池。无盈利承诺。
