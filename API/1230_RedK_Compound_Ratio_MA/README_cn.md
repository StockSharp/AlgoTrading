# Redk Compound Ratio MA 策略
[English](README.md) | [Русский](README_ru.md)

当复合比率移动平均线 (CoRa Wave) 上升时做多，下跌时做空。

## 详情

- **入场条件**:
  - 多头: CoRa Wave 高于前一值
  - 空头: CoRa Wave 低于前一值
- **多空方向**: 双向
- **出场条件**:
  - 反向信号
- **止损**: 无
- **默认参数**:
  - `Length` = 20
  - `RatioMultiplier` = 2m
  - `AutoSmoothing` = true
  - `ManualSmoothing` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: Compound Ratio MA, Weighted Moving Average
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 无
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
