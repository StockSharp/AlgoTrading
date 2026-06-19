# IU 大实体柱策略
[Русский](README_ru.md) | [English](README.md)

该策略在当前K线实体比过去20根K线平均实体大数倍时开仓。大阳线开多头，大阴线开空头。仓位通过基于ATR的跟踪止损保护。

## 细节

- **入场条件**：
  - 多头：实体 > 平均实体 * BigBodyThreshold 且 收盘价 > 开盘价。
  - 空头：实体 > 平均实体 * BigBodyThreshold 且 收盘价 < 开盘价。
- **多空方向**：双向。
- **退出条件**：ATR 跟踪止损。
- **止损**：使用 ATR * AtrFactor 的跟踪止损。
- **默认参数**：
  - `BigBodyThreshold` = 4
  - `AtrLength` = 14
  - `AtrFactor` = 2
  - `CandleType` = 5 分钟
- **过滤器**：
  - 类别：动量
  - 方向：双向
  - 指标：SMA，ATR
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

