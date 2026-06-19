# Weighted Harrell-Davis Quantile Estimator with AbsoluteDeviation 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用基于中位数的分位估计和绝对偏差带来检测价格异常。
当收盘价跌破下轨时买入，涨破上轨时卖出。

## 细节

- **入场条件**: 收盘价低于下轨或高于上轨
- **多空方向**: 双向
- **出场条件**: 穿越相反的轨道
- **止损**: 无
- **默认值**:
  - `Length` = 39
  - `DeviationMultiplier` = 1.213
- **过滤器**:
  - 分类: 均值回归
  - 方向: 双向
  - 指标: Median
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

