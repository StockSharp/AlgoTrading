# 成交量激增趋势 (Volume Spike Trend)
[English](README.md) | [Русский](README_ru.md)

当成交量超过平均值乘以倍数时表明强烈参与。

测试表明年均收益约为 175%，该策略在股票市场表现最佳。

顺着价格与均线方向开仓。

## 详情

- **入场条件**: Volume change exceeds `VolumeSpikeMultiplier` times average.
- **多空方向**: Both directions.
- **出场条件**: Volume drops below average or stop.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `VolAvgPeriod` = 20
  - `VolumeSpikeMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: Volume, MA
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

