# Average High-Low Range IBS Reversal策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格持续低于基于高低价平均范围的动态阈值时寻找均值回归。它计算条形高低区间的移动平均值，以及在回溯期内的最高价和最低价。买入阈值定义为最高价减去2.5倍的平均范围。当价格在该水平下方维持指定数量的柱，并且柱内强度(IBS)在交易窗口内低于设定值时，开立多头仓位。若收盘价超过前一根柱的最高价，则平仓。

## 细节

- **入场条件**：
  - 价格在 `BarsBelowThreshold` 根柱内均低于买入阈值。
  - IBS < `IbsBuyThreshold`。
  - 时间位于 `StartTime` 与 `EndTime` 之间。
- **方向**：仅做多。
- **出场条件**：
  - 收盘价高于前一根柱的最高价。
- **止损**：无。
- **默认参数**：
  - `Length` = 20
  - `BarsBelowThreshold` = 2
  - `IbsBuyThreshold` = 0.2
- **过滤器**：
  - 类型：均值回归
  - 方向：多头
  - 指标：SMA, Highest, Lowest
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：低
