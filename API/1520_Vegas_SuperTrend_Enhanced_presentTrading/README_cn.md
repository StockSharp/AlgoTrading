# Vegas SuperTrend Enhanced 策略
[English](README.md) | [Русский](README_ru.md)

结合 Vegas 通道与调整后的 SuperTrend。
当 SuperTrend 方向翻转时入场，乘数随波动宽度调整。

## 细节

- **入场条件**: 调整后的 SuperTrend 方向改变
- **多空方向**: 双向（可配置）
- **出场条件**: 反向趋势翻转
- **止损**: 无
- **默认值**:
  - `AtrPeriod` = 10
  - `VegasWindow` = 100
  - `SuperTrendMultiplier` = 5
  - `VolatilityAdjustment` = 5
  - `TradeDirection` = "Both"
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: ATR, SMA, StandardDeviation
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
