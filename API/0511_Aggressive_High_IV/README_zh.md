# 高IV市场的激进策略
[English](README.md) | [Русский](README_ru.md)

该策略结合EMA交叉与ATR波动率过滤。只有当波动率高于其均值加上一倍标准差时才进场，并以ATR为基础设定止盈止损。

测试显示在高波动市场中表现稳定。

在波动性升高时通过EMA交叉开仓，目标是在预先控制风险的情况下迅速获利。

仓位通过ATR倍数的止损和止盈来平仓。

## 详情

- **入场条件**: 快速EMA上穿或下穿慢速EMA且ATR > 平均值 + 标准差。
- **多空方向**: 双向。
- **出场条件**: ATR止损或止盈触发。
- **止损**: 有。
- **默认值**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 30
  - `AtrLength` = 14
  - `AtrMeanLength` = 20
  - `AtrStdLength` = 20
  - `RiskFactor` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**:
  - 类别: Trend
  - 方向: Both
  - 指标: EMA, ATR
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: High
