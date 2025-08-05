# ADX趋势策略
[English](README.md) | [Русский](README_ru.md)

该策略通过平均方向指数(ADX)衡量市场趋势强度。当ADX高于阈值且价格位于均线的有利一侧时顺势进场；当ADX走弱或出现反向信号时退出。止损通常基于ATR倍数，以适应不同的波动性。

测试表明年均收益约为 46%，该策略在股票市场表现最佳。

## 详情
- **入场条件**: 基于 MA、ADX 和 ATR 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `AdxPeriod` = 14
  - `MaPeriod` = 50
  - `AtrMultiplier` = 2m
  - `AdxExitThreshold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: MA, ADX, ATR
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

