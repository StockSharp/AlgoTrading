# Time
[Русский](README_ru.md) | [English](README.md)

该策略展示时间函数。当最高价相对开盘价超过指定tick并持续一段时间后买入。

## 细节

- **入场条件**：最高价与开盘价之差在设定秒数内保持超过阈值。
- **多/空**：仅做多。
- **出场条件**：条件不再满足。
- **止损**：无。
- **默认参数**：
  - `TicksFromOpen` = 0
  - `SecondsCondition` = 20
  - `ResetOnNewBar` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类别: 突破
  - 方向: 多
  - 指标: 价格
  - 止损: 无
  - 复杂度: 基础
  - 周期: 日内 (1m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
