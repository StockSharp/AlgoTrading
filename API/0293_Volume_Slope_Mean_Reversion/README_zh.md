# Volume Slope Mean Reversion
[English](README.md) | [Русский](README_ru.md)

Volume Slope Mean Reversion 策略关注指标的极端读数以捕捉均值回归。远离正常水平的情况通常不会持续太久。

当指标大幅偏离均值后开始反转时产生交易信号，可做多也可做空，并带有保护性止损。

适合预期震荡行情的交易者，当指标回归平衡时平仓。初始参数 `VolumeMaPeriod` = 20.

## 详细信息

- **入场条件**: Indicator crosses back toward mean.
- **多空**: Both directions.
- **出场条件**: Indicator reverts to average.
- **止损**: Yes.
- **默认值**:
  - `VolumeMaPeriod` = 20
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 分类: Mean Reversion
  - 方向: Both
  - 指标: Volume
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Short-term
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险级别: Medium