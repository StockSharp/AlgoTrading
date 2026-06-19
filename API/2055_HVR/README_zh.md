# 历史波动率比率策略
[English](README.md) | [Русский](README_ru.md)

该策略基于历史波动率比率 (HVR)。利用对数收益率计算6根K线的短期波动率与100根K线的长期波动率之比。当比率高于阈值时，系统做多，预期波动率扩张；当比率低于阈值时，系统做空。

## 细节

- **入场条件**：
  - 多头：`HVR > RatioThreshold`
  - 空头：`HVR < RatioThreshold`
- **多/空**：双向
- **出场条件**：相反信号
- **止损**：无
- **默认值**：
  - `ShortPeriod` = 6
  - `LongPeriod` = 100
  - `RatioThreshold` = 1.0
  - `CandleType` = `TimeSpan.FromMinutes(15).TimeFrame()`
- **过滤器**：
  - 类别：波动率
  - 方向：双向
  - 指标：历史波动率（短期与长期）
  - 止损：无
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险水平：中等
