# One-Two-Three Pattern 策略

## 概述

本策略复刻了 Martes 在 MetaTrader 4 中的专家顾问“1-2-3_forCodeBase_v01.mq4”。它在收盘价上寻找经典的 1-2-3 反转形态：两个连续的趋势段配合一个回撤段。移植版本完整保留了原有规则，包括自定义的趋势长度指标（`RelDownTrLen_forCodeBase_v01`、`RelUpTrLen_forCodeBase_v01`）以及 MACD 过滤条件。

做多信号要求：当前价格附近出现新的低点（点 3），其前方存在高点（点 2），再往前有低点（点 1）。上一段下跌趋势的长度必须至少是当前上行回撤的 `TrendRatio` 倍，同时 MACD 需向上穿越信号线（或零轴），并且在点 3 处保持为正值。做空逻辑完全镜像。止损放在点 3 之外一个最小跳动，止盈等于上一段摆动的高度；若启用按点数的追踪止损，则当浮盈达到设定距离时向有利方向移动止损。

## 交易规则

1. 订阅指定类型的 K 线（`CandleType`），并按收盘价计算 MACD（快/慢/信号周期）。
2. 维护一个滚动的 K 线实体缓冲区，用于识别 1-2-3 结构。实体最低点视为谷值，实体最高点视为峰值。
3. 按照原始自定义指标的凸包算法计算趋势长度，并将结果归一化到 `[0,1]`。多头要求最近的下跌段长度至少是上一段上升段的 `TrendRatio` 倍（空头反之）。
4. 用 MACD 确认信号：
   - 多头：MACD 向上穿越信号线（或零轴），且点 3 处的 MACD 值为正。
   - 空头：MACD 向下穿越信号线（或零轴），且点 3 处的 MACD 值为负。
5. 额外过滤条件：
   - 当前价格距离点 2 不得超过 5 个点。
   - 预期止损距离 `|point2 - point3|` 必须不少于 13 个点。
   - `TakeProfitPips` 保持 ≥ 10，否则策略停止交易（与原版相同的安全检查）。
6. 委托管理：
   - 使用 `BuyMarket`/`SellMarket` 按 `TradeVolume` 手数下单（若反手交易，会把当前仓位数量加上）。
   - 初始止损 = 点 3 ± 一个最小价格步长。
   - 止盈 = 入场价 ± `|point2 - point3|`。
   - 当 `TrailingStopPips` > 0 且浮盈超过该距离后，将止损沿趋势方向移动相同的点数。
7. 当触发止损、止盈或追踪止损时平仓。策略同一时间只持有一个方向的仓位。

## 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `TakeProfitPips` | `decimal` | `60` | 与原版一致的兼容参数，若小于 10 则禁止交易。 |
| `TradeVolume` | `decimal` | `0.5` | 每次下单的 MetaTrader 手数。 |
| `TrailingStopPips` | `decimal` | `30` | 追踪止损距离（点）。设为 `0` 关闭追踪。 |
| `TrendRatio` | `decimal` | `4` | 主趋势长度与回撤长度的最小比值。 |
| `CandleType` | `DataType` | `H1` | 用于识别形态和计算 MACD 的 K 线类型。 |
| `MacdFast` | `int` | `12` | MACD 快速 EMA 周期。 |
| `MacdSlow` | `int` | `26` | MACD 慢速 EMA 周期。 |
| `MacdSignal` | `int` | `9` | MACD 信号线周期。 |
| `PatternLookback` | `int` | `100` | 搜索 1-2-3 形态时最多回溯的历史 K 线数量。 |

## 实现说明

- `RelDownTrLen` 与 `RelUpTrLen` 指标逻辑一字不差地移植：对 K 线实体的凸包寻找最长单调区间，并输出其相对长度（`[0,1]`）。该值用于趋势强度过滤。
- 为控制内存开销，历史 K 线与 MACD 值分别保存在长度最多 600 的环形缓冲中，同时保证足够的分析深度。
- 止损与止盈完全按 MetaTrader 的方式手动维护：与当根 K 线的高/低比较，追踪止损只有在价格前进到设定距离后才会收紧。
- 在 `OnReseted` 与 `OnStarted` 中同步 `Volume = TradeVolume`，以便优化器沿用标准的策略属性。

## 参考

- 原始 MQL4 策略：`MQL/8131/1-2-3_forCodeBase_v01.mq4`。
- 自定义指标：`RelDownTrLen_forCodeBase_v01.mq4`、`RelUpTrLen_forCodeBase_v01.mq4`。
