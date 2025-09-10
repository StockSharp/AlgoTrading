# 突破回测策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格突破最近高点或低点时入场，并可在回测时入场，同时使用移动止损管理风险。

策略通过回溯窗口内的最高收盘价和最低收盘价来定义支撑与阻力。突破后可立即开仓或等待价格回测被突破水平。一旦达到指定盈利阈值，初始止损将转为追踪止损。

## 详情
- **入场条件**：突破支撑或阻力，可选回测入场。
- **多空方向**：可配置。
- **退出条件**：追踪止损或反向突破。
- **止损**：初始止损和追踪止损。
- **默认值**:
  - `LookbackPeriod` = 20
  - `RetestBarsSinceBreakout` = 2
  - `RetestDetectionLimit` = 2
  - `ProfitThresholdPercent` = 5m
  - `TrailingStopGapPercent` = 1m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 突破
  - 方向: 双向
  - 指标: Highest, Lowest
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
