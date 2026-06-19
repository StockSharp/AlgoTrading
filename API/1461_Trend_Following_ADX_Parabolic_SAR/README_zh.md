# 趋势跟随 ADX Parabolic SAR
[English](README.md) | [Русский](README_ru.md)

该策略结合 ADX 与 Parabolic SAR 以捕捉趋势。当 ADX 高于阈值、+DI 大于 -DI 且价格在 SAR 之上时做多，反向条件下做空。

## 详情

- **入场条件**: ADX > 阈值 且 +DI > -DI 且 价格 > SAR
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 否
- **默认值**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: ADX, Parabolic SAR
  - 止损: 否
  - 复杂度: 基础
  - 时间框架: 日内 (1m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中
