# Fast2 Crossover 策略
[Русский](README_ru.md) | [English](README.md)

基于 Fast2 直方图的策略。直方图把最近三根K线的实体按平方根权重组合，并用两条加权移动平均线平滑。当快线下穿慢线时做多，快线上穿慢线时做空。

## 详情

- **入场条件**：
  - 多头：快线下穿慢线
  - 空头：快线上穿慢线
- **多空**：双向
- **出场条件**：
  - 相反的交叉
- **止损**：无
- **默认值**：
  - `FastLength` = 3
  - `SlowLength` = 9
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **过滤器**：
  - 类型: 交叉
  - 方向: 双向
  - 指标: WeightedMovingAverage
  - 止损: 否
  - 复杂度: 基础
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险级别: 中
