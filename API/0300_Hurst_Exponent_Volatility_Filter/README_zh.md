# Hurst Exponent Volatility Filter
[English](README.md) | [Русский](README_ru.md)

Hurst Exponent Volatility Filter 策略结合指标与波动率过滤器，只在满足特定条件时进场。

测试表明年均收益约为 163%，该策略在股票市场表现最佳。

信号要求指标超过设定阈值且波动率符合预设标准，可做多或做空，并带有止损。

策略专为重视风险控制的交易者设计，一旦指标回归均值或波动率变化便退出。初始设置 `HurstPeriod` = 100.

## 详细信息

- **入场条件**: Indicator crosses back toward mean.
- **多空**: Both directions.
- **出场条件**: Indicator reverts to average.
- **止损**: Yes.
- **默认值**:
  - `HurstPeriod` = 100
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `StopLoss` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 分类: Mean Reversion
  - 方向: Both
  - 指标: Hurst
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Short-term
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险级别: Medium
