# SilverTrend Signal ReOpen 策略
[English](README.md) | [Русский](README_ru.md)

基于 SilverTrend 指标并带有加仓功能的策略。当指标改变方向时开仓，并在价格按设定步长朝持仓方向移动时加仓。仓位可在相反信号或触发止损/止盈时平仓。

## 详情

- **入场条件**:
  - 多头：SilverTrend 指标由下降趋势转为上升趋势
  - 空头：SilverTrend 指标由上升趋势转为下降趋势
- **多空**: 都支持
- **离场条件**:
  - 可选地在相反的 SilverTrend 信号下平仓
  - 触发止损或止盈
- **止损**: 绝对价格水平
- **默认值**:
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Ssp` = 9
  - `Risk` = 3
  - `PriceStep` = 300m
  - `PosTotal` = 10
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **过滤器**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: SilverTrend
  - 止损: 是
  - 复杂度: 中等
  - 周期: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
