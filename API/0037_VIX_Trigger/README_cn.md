# VIX触发 (VIX Trigger)
[English](README.md) | [Русский](README_ru.md)

监控波动率指数VIX的变化。上升的VIX可能预示反转。

结合价格与均线位置决定方向。

## 详情

- **入场条件**: VIX rising while price relative to MA triggers longs or shorts.
- **多空方向**: Both directions.
- **出场条件**: VIX falls or stop.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Contrarian
  - 方向: Both
  - 指标: VIX, MA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
