# Pure Price Action Strategy 纯价格行为策略
[English](README.md) | [Русский](README_ru.md)

该策略基于近期高低点检测结构突破 (BOS) 和结构转变 (MSS)。

当出现 BOS 时做多，出现 MSS 时做空，并使用固定百分比的止损和止盈。

## 详情
- **入场条件**: BOS 做多，MSS 做空
- **多空方向**: 双向
- **退出条件**: 止损或止盈
- **止损**: 固定百分比
- **默认值**:
  - `Length` = 5
  - `SlPercent` = 1m
  - `TpPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类型: Price Action
  - 方向: 双向
  - 指标: Highest, Lowest
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (1m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中
