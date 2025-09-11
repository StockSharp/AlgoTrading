# FTMO Rules Monitor
[English](README.md) | [Русский](README_ru.md)

基于 ATR 的 FTMO 规则监控策略。

策略通过 ATR 计算仓位，并在满足挑战条件后停止。它监控每日最大亏损、总亏损、盈利目标和最少交易天数。

## 详情

- **入场条件**：阳线做多，阴线做空。
- **多空方向**：双向。
- **出场条件**：挑战完成或反向信号。
- **止损**：ATR。
- **默认值**:
  - `AccountSize` = 10000m
  - `RiskPercent` = 1m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别：风险管理
  - 方向：双向
  - 指标：ATR
  - 止损：ATR
  - 复杂度：基础
  - 时间框架：日内 (5m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中等
