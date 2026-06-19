# 流动性突破策略
[English](README.md) | [Русский](README_ru.md)

该策略利用最近高低点构成的价格区间，当收盘价突破上沿或下沿时开仓。止损可选用 SuperTrend 线或固定百分比。

## 详情

- **入场条件**:
  - `收盘价 > 前高` → 做多
  - `收盘价 < 前低` → 做空
- **多空方向**: 可配置（多、空、双向）。
- **出场条件**: 反向突破或止损。
- **止损**: SuperTrend 或固定百分比。
- **默认参数**:
  - `PivotLength` = 12
  - `StopLoss` = SuperTrend
  - `FixedPercentage` = 0.1
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **过滤器**:
  - 分类: 突破
  - 方向: 双向
  - 指标: Highest, Lowest, SuperTrend
  - 止损: 可选
  - 复杂度: 低
  - 时间框架: 1小时
