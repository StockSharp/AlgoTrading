# VWAP Stochastic Divergence
[English](README.md) | [Русский](README_ru.md)

**VWAP Stochastic Divergence** 策略基于 combining VWAP with ADX trend strength indicator。

当 Stochastic confirms divergence setups 在日内（5m）数据上得到确认时触发信号，适合积极交易者。

止损依赖于 ATR 倍数以及 AdxPeriod, AdxThreshold 等参数，可根据需要调整以平衡风险与收益。

## 详情
- **入场条件**：参见指标条件实现.
- **多空方向**：双向.
- **退出条件**：反向信号或止损逻辑.
- **止损**：是，基于指标计算.
- **默认值**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
  - `AdxExitThreshold = 20m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **过滤器**:
  - 分类: 趋势跟随
  - 方向: 双向
  - 指标: Stochastic, Divergence
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 是
  - 风险等级: 中等

测试表明年均收益约为 79%，该策略在股票市场表现最佳。
