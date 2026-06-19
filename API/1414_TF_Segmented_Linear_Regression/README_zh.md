# TF Segmented Linear Regression
[English](README.md) | [Русский](README_ru.md)

该策略在每个时间段内构建线性回归通道。当价格突破上轨时做多，跌破下轨时做空。

## 详情
- **入场条件**: 价格突破回归通道。
- **多空方向**: 双向。
- **退出条件**: 穿越相反边界。
- **止损**: 无。
- **默认值**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `Segment` = TimeSpan.FromDays(1)
  - `Multiplier` = 2
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: Linear Regression
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
