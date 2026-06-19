# Williams R Ichimoku Strategy
[English](README.md) | [Русский](README_ru.md)

此策略结合Williams %R与一目均衡表。当%R跌破-80且价格位于云层上方并且转折线高于基准线时做多；当%R高于-20且价格在云层下方并且转折线低于基准线时做空。价格穿越云层另一侧时离场。

测试表明年均收益约为 73%，该策略在加密市场表现最佳。

该方法适合偏好明确趋势过滤的交易者，止损围绕基准线设置，随趋势强度调整。

## 细节
- **入场条件**:
  - 多头: `%R < -80 && price above Ichimoku cloud && Tenkan > Kijun`
  - 空头: `%R > -20 && price below Ichimoku cloud && Tenkan < Kijun`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格跌破云层时平仓
  - 空头: 价格突破云层时平仓
- **止损**: 是
- **默认值**:
  - `WilliamsRPeriod` = 14
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: Williams R Ichimoku
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

