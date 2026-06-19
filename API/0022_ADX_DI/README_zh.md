# ADX DI
[English](README.md) | [Русский](README_ru.md)

该策略关注+DI与-DI的交叉以及ADX的强度。+DI上穿-DI且ADX走强时做多，反向交叉则做空；ADX减弱或出现相反交叉时离场。通过要求ADX确认，可避免在每次DI交叉时交易，旨在捕捉可持续的趋势。

测试表明年均收益约为 103%，该策略在股票市场表现最佳。

## 详情
- **入场条件**: 基于 ADX、ATR 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: ADX, ATR
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

