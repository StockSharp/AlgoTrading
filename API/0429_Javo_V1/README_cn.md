# Javo v1 策略
[English](README.md) | [Русский](README_ru.md)

Javo v1 将 Heikin Ashi 蜡烛与两条 EMA 结合。当 HA 方向与快慢 EMA 的交叉一致时开仓，旨在捕捉新出现的趋势并减少噪声。

## 详情

- **入场条件**:
  - **多头**: HA 看涨且 `EMA_fast > EMA_slow`
  - **空头**: HA 看跌且 `EMA_fast < EMA_slow`
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `FastEmaPeriod` = 1
  - `SlowEmaPeriod` = 30
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 双向
  - 指标: Heikin Ashi, EMA
  - 止损: 无
  - 复杂度: 低
  - 时间框架: 小时级
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
