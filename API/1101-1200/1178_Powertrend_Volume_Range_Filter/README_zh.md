# Powertrend - Volume Range Filter 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略使用基于成交量的范围过滤器来计算动态价格区间。价格在上升趋势中突破上轨时开多，当价格跌破下轨时平仓。可选过滤器包括 ADX、趋势高低点确认和 VWMA。

## 计算与执行

C# 和 Python 使用以下明确的因果定义。在已完成K线上，`d = abs(Close - 前一 Close)`，`V = TotalVolume`。取得 `RangePeriod` 次变化后，在包含当前K线的窗口上计算 `r = RangeMultiplier * sum(d * V) / sum(V)`，总成交量必须为正。首个已形成的过滤值 `f` 为当前收盘价；随后把前一个 `f` 限制到 `[Close - r, Close + r]` 内。当前决策的上下轨为更新中心前的 `前一 f + r` 与 `前一 f - r`，避免移动过滤值后将上轨定义成突破价格本身。

仅在空仓、`Close > 上轨` 且过滤值上升时，以 `Volume` 入场。当 `Close < 下轨` 时，用市价单平掉全部多仓，不受入场过滤器限制。同价保持仓位，未完成订单阻止追加订单。没有做空、EMA 交叉或冷却期。

可选 ADX 必须已形成且严格大于 `AdxThreshold`；禁用时不注册该指标，不等待其预热。高低点确认同时要求 `当前 High > 前一 High` 和 `当前 Low > 前一 Low`。过滤值 VWMA 为最近 `VwmaPeriod` 个已形成过滤值的 `sum(f * V) / sum(V)`，包含当前值；要求完整窗口、正的总成交量和 `Close > VWMA`。关闭的过滤器没有预热要求。这些是收盘时的市价决策，不是交易所止损单，也不假设在K线内部最高/最低价成交。

默认：`RangePeriod=40`、`RangeMultiplier=1.5`、`AdxPeriod=14`、`AdxThreshold=20`、`VwmaPeriod=20`、`CandleType=TimeSpan.FromMinutes(5).TimeFrame()`；`IsAdxFilterEnabled`、`IsHighLowFilterEnabled`、`IsVwmaFilterEnabled` 均为 `false`。周期和倍数必须为正，ADX 阈值在 `[0, 100]` 内。这些公式和默认值明确了本示例；原简介没有定义原始指标公式。
