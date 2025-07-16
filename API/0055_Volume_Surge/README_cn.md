# 成交量激增 (Volume Surge)
[English](README.md) | [Русский](README_ru.md)

监控成交量远高于均值的情况, 表示市场强烈兴趣。

激增时顺趋势开仓, 成交量回落或止损退出。

## 详情

- **入场条件**: Volume ratio above `VolumeSurgeMultiplier`.
- **多空方向**: Both directions.
- **出场条件**: Volume drops below average or stop.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `VolumeAvgPeriod` = 20
  - `VolumeSurgeMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: Volume
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
