# Sigma Spike Filtered Binned OPR 策略
[English](README.md) | [Русский](README_ru.md)

Sigma Spike Filtered Binned OPR 收集 OPR（开盘位置比率）的分布，并在收益出现 Sigma 突变后 OPR 达到极端区间时进行交易。

## 细节

- **入场条件**: OPR 位于极端区间 (<= `OprThreshold` 或 >= `100 - OprThreshold`)，可选的 sigma 突变过滤
- **多空方向**: 双向
- **出场条件**: 相反信号
- **止损**: 无
- **默认值**:
  - `SigmaSpikeLength` = 20
  - `FilterBySigmaSpike` = true
  - `SigmaSpikeThreshold` = 2
  - `OprThreshold` = 10
- **过滤器**:
  - 分类: 形态
  - 方向: 双向
  - 指标: StandardDeviation
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
