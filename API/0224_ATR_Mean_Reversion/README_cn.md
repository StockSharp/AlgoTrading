# ATR Mean Reversion Strategy

此策略衡量价格偏离移动平均线的距离与近期波动(ATR)的比值。价格偏离越大，ATR阈值越宽，动态适应市场活跃程度。

当收盘价低于均线`Multiplier*ATR`以上时做多；当收盘价高于均线同样距离时做空。价格回到均线时离场。ATR止损保持风险与当前波动相匹配。

适合短线交易者，在过度波动后期望价格回归均值。

## 细节
- **入场条件**:
  - 多头: `Close < MA - Multiplier * ATR`
  - 空头: `Close > MA + Multiplier * ATR`
- **多/空**: 双向
- **离场条件**:
  - 多头: 收盘价>= MA
  - 空头: 收盘价<= MA
- **止损**: 默认约`2*ATR`
- **默认值**:
  - `MaPeriod` = 20
  - `AtrPeriod` = 14
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mean Reversion
  - 方向: 双向
  - 指标: MA, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
