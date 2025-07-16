# Beta Adjusted Pairs Trading
[English](README.md) | [Русский](README_ru.md)

Beta Adjusted Pairs Trading 策略结合指标与波动率过滤器，只在满足特定条件时进场。

信号要求指标超过设定阈值且波动率符合预设标准，可做多或做空，并带有止损。

策略专为重视风险控制的交易者设计，一旦指标回归均值或波动率变化便退出。初始设置 `Asset2` = (Security.

## 详细信息

- **入场条件**: Indicator crosses back toward mean.
- **多空**: Both directions.
- **出场条件**: Indicator reverts to average.
- **止损**: Yes.
- **默认值**:
  - `Asset2` = (Security
  - `Asset2Portfolio` = (Portfolio
  - `BetaAsset1` = 1.0m
  - `BetaAsset2` = 1.0m
  - `LookbackPeriod` = 20
  - `EntryThreshold` = 2.0m
  - `StopLoss` = 2.0m
- **过滤器**:
  - 分类: Mean Reversion
  - 方向: Both
  - 指标: Beta
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Short-term
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险级别: Medium