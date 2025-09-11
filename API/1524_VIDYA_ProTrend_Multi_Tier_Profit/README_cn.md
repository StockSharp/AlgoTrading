# VIDYA ProTrend Multi-Tier Profit 策略
[English](README.md) | [Русский](README_ru.md)

该趋势策略结合快慢 VIDYA 与布林带过滤器。
可选地根据 ATR 倍数和百分比目标设置多级止盈单。

## 细节

- **入场条件**: 快 VIDYA 高于慢 VIDYA 且价格突破布林带
- **多空方向**: 双向
- **出场条件**: 反向坡度或交叉
- **止损**: 无
- **默认值**:
  - `FastVidyaLength` = 10
  - `SlowVidyaLength` = 30
  - `MinSlopeThreshold` = 0.05
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: VIDYA, 布林带, ATR
  - 止损: 无
  - 复杂度: 高级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
