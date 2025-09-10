# CCI 支撑阻力策略
[English](README.md) | [Русский](README_ru.md)

该策略利用 CCI 枢轴构建动态支撑和阻力水平，并在交易这些水平的突破前使用基于 EMA 交叉或斜率的趋势过滤。

## 详情

- **入场条件**：
  - 多头：价格触及基于 CCI 的支撑并收盘价上破，且趋势看多。
  - 空头：价格触及基于 CCI 的阻力并收盘价下破，且趋势看空。
- **多空方向**：双向。
- **出场条件**：
  - 基于 ATR 的止损与止盈。
- **止损**：是，基于 ATR。
- **默认参数**：
  - `CciLength` = 50
  - `LeftPivot` = 50
  - `RightPivot` = 50
  - `Buffer` = 10
  - `TrendMatter` = true
  - `TrendType` = Cross
  - `SlowMaLength` = 100
  - `FastMaLength` = 50
  - `SlopeLength` = 5
  - `Ksl` = 1.1
  - `Ktp` = 2.2
- **过滤器**：
  - 类型: 突破
  - 方向: 双向
  - 指标: CCI, EMA, ATR
  - 止损: 是
  - 复杂度: 中
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险级别: 中等
