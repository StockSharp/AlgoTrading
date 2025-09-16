# Maximus vX Lite 策略
[English](README.md) | [Русский](README_ru.md)

将 MetaTrader 5 顾问 "maximus_vX lite" 转换为 StockSharp 高层 API 的实现。
策略会在当前价格上下寻找盘整区，等待价格离开该区间一定的点数后再进场。仓位大小可以根据
可选的风险百分比预算计算，当浮动利润达到阈值时会强制平掉所有仓位。

## 策略逻辑

1. **历史扫描**：每根收盘 K 线后保留最多 `HistoryDepth` 根历史 K 线，通过 `RangeLookback` 长度的滑动窗口
   搜索满足条件的局部高点和低点，从而构建盘整区间。
2. **上方通道**：当检测到合格的上方盘整块时，以当前收盘价为中心，使用 `RangePoints` 的宽度构建上轨。
   如果历史数据中没有满足条件的块，则退化为围绕当前价格的同宽度通道。
3. **下方通道**：如果历史中存在符合条件的块，则直接使用其高点/低点；否则在当前收盘价下方
   `RangePoints` 的位置构建一个合成通道。
4. **多头入场**：允许两种多头情形：
   - 突破下方盘整：价格必须比 `_lowerMax` 高出 `DistancePoints`，并且上方通道已建立。止盈取 `_lowerMax`
     与 `_upperMin` 之间距离的三分之二，且不低于 `RangePoints`。
   - 突破上方通道：价格比 `_upperMax` 高出 `DistancePoints`。止盈设置为 `2 * RangePoints`。
5. **空头入场**：当价格比 `_upperMin` 或 `_lowerMin` 低 `DistancePoints` 时触发，对应的止盈逻辑与多头对称，
   主信号使用三分之二的动态目标，次信号使用 `2 * RangePoints`。
6. **止损与退出**：`StopLossPoints`>0 时启用固定止损。`MinProfitPercent` 监控浮动权益与最近空仓时的余额，
   一旦超过阈值立即平仓。策略内部手动检查止损/止盈，以复现原始 EA 的行为。
7. **头寸规模**：当设置了 `RiskPercent` 并且存在止损时，根据组合价值和止损距离计算下单数量；
   否则使用默认的 `Volume` 数量。

## 参数

- `DelayOpen`（默认 `2`）：允许在同方向加仓的时间框数量。
- `DistancePoints`（默认 `850`）：距离盘整边界至少多少点后才允许进场。
- `RangePoints`（默认 `500`）：盘整区的宽度。
- `HistoryDepth`（默认 `1000`）：保存的历史 K 线数量。
- `RangeLookback`（默认 `40`）：计算局部高低点的窗口长度。
- `CandleType`（默认 `TimeSpan.FromMinutes(15).TimeFrame()`）：使用的时间框。
- `RiskPercent`（默认 `5m`）：单笔交易的风险占组合价值的百分比，设为 0 表示使用固定手数。
- `StopLossPoints`（默认 `1000`）：止损距离；设为 0 时不启用止损。
- `MinProfitPercent`（默认 `1m`）：浮动利润达到该百分比时强制平仓。

## 其他信息

- **方向**：多空双向
- **退出方式**：固定止损/止盈以及 `MinProfitPercent` 触发的强制平仓
- **止损**：可选的 `StopLossPoints` 固定止损
- **指标**：无（完全依赖价格与滑动窗口分析）
- **时间框**：通过 `CandleType` 设置（默认 15 分钟）
- **复杂度**：中等（结合历史扫描、动态止盈与风险控制）
- **风险等级**：若启用风险百分比，因突破特性风险偏高
