# GBPCHF 双MACD 相关策略

## 概述
该策略交易 GBP/CHF 汇率，通过比较 GBPUSD 与 USDCHF 的 MACD 指标来判断整体英镑强弱。两条 MACD 线均使用相同的时间框架（默认 1 小时）计算，参数为 12、26、9。当 GBPUSD 的 MACD 线在正区间向上穿越 USDCHF 的 MACD 线时，视为英镑同时对美元和瑞郎走强，策略买入 GBPCHF；当两条线位于负区间且 GBPUSD MACD 向下穿越 USDCHF MACD 时，策略做空。`ReverseSignals` 可反向操作。

## 执行流程
1. 订阅 GBPCHF、GBPUSD、USDCHF 的蜡烛数据。
2. 在 GBPUSD 与 USDCHF 上绑定 MACD 指标，并等待指标形成。
3. 信号生成：
   - **做多**：前一根蜡烛两条 MACD 线均为正且 GBPUSD MACD 从下向上突破 USDCHF MACD。
   - **做空**：前一根蜡烛两条 MACD 线均为负且 GBPUSD MACD 从上向下跌破 USDCHF MACD。
4. 根据 `OnlyOnePosition` 与 `CloseOppositePositions` 控制是否允许同时持有多头与空头以及是否先平掉反向仓位。
5. 通过固定止损、止盈及可选的追踪止损管理风险。

## 参数说明
- `StopLossPips`：初始止损距离（点）。
- `TakeProfitPips`：止盈距离（点）。
- `TrailingFrequencySeconds`：追踪止损的更新时间间隔，低于 10 时仅在新蜡烛生成时调整。
- `TrailingStopPips`：追踪止损距离，设为 0 关闭追踪功能。
- `TrailingStepPips`：价格向有利方向移动的最小点数，用于触发追踪止损的上移。
- `ReverseSignals`：反转信号方向。
- `CloseOppositePositions`：在开仓前是否平掉反向仓位。
- `OnlyOnePosition`：限制同一时间只能持有一个方向的仓位。
- `TradeVolume`：开仓手数。
- `MacdShortPeriod`、`MacdLongPeriod`、`MacdSignalPeriod`：MACD 参数。
- `CandleType`：用于计算的蜡烛类型。
- `GbpUsdSecurity`、`UsdChfSecurity`：GBPUSD 与 USDCHF 的证券对象（必填）。

## 风险控制
- 首选使用 `StopLossPips` 设置固定止损；若为 0，则使用 `TrailingStopPips` 作为初始保护距离。
- `TakeProfitPips` 大于 0 时设定固定止盈。
- 追踪止损在价格达到 `TrailingStopPips` 且满足 `TrailingStepPips` 与更新时间条件后上移，始终使用市场单出场。

## 使用建议
- 确保三个证券的蜡烛数据使用同一时间框架，否则相关判断会失真。
- 若交易账户不允许对冲，建议开启 `CloseOppositePositions`。
- 追踪止损基于已完成的蜡烛；如需更快响应，可使用更小的时间框架或减少更新时间间隔。
