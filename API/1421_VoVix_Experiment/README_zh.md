# The VoVix Experiment 策略
[English](README.md) | [Русский](README_ru.md)

该策略分析快慢 ATR 的比值。当该比值的 z-score 激增并达到局部最大值时，根据蜡烛方向开仓。当 z-score 低于退出阈值时平仓。

## 细节

- **入场条件**: VoVix z-score 高于 `EntryZ` 且处于局部最大
- **多空方向**: 双向
- **出场条件**: VoVix z-score 低于 `ExitZ`
- **止损**: 无
- **默认值**:
  - `FastAtrLength` = 13
  - `SlowAtrLength` = 26
  - `ZScoreWindow` = 81
  - `EntryZ` = 1.0
  - `ExitZ` = 1.4
  - `LocalMaxWindow` = 6
  - `SuperZ` = 2.0
  - `MinVolume` = 1
  - `MaxVolume` = 2
- **过滤器**:
  - 分类: 波动率
  - 方向: 双向
  - 指标: ATR, Highest, SMA, StdDev
  - 止损: 无
  - 复杂度: 高级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
