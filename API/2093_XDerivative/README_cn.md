# XDerivative 策略
[English](README.md) | [Русский](README_ru.md)

XDerivative 策略通过平滑的价格变化率来捕捉动量变化。原始的 MQL 专家将 Rate of Change 与 Jurik 平滑相结合来识别转折点。该版本使用 StockSharp 的内置指标实现相同的概念。

策略计算 `RocPeriod` 根柱的 Rate of Change，并使用长度为 `MaLength` 的 Jurik 移动平均进行平滑。当平滑后的导数在局部低点后向上转折时，策略开多或翻多；当在局部高点后向下转折时，策略开空或翻空。仓位由百分比止盈和止损管理。

## 详情

- **入场条件**:
  - 多头: 平滑导数在局部低点后向上转折。
  - 空头: 平滑导数在局部高点后向下转折。
- **多空方向**: Both directions.
- **出场条件**: 反向转折或保护止损。
- **止损**: Yes.
- **默认值**:
  - `RocPeriod` = 34
  - `MaLength` = 7
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**:
  - 类别: Trend following
  - 方向: Both
  - 指标: RateOfChange, JurikMovingAverage
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: 4H
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
