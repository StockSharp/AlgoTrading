# Renko趋势反转策略
[Русский](README_ru.md) | [English](README.md)

Renko趋势反转策略在Renko开盘价与收盘价交叉时交易，可选止损和止盈，使用基于ATR的Renko砖。

## 详情

- **入场条件**：Renko开盘/收盘交叉并在时间窗口内
- **多空方向**：双向
- **出场条件**：可选止损或止盈
- **止损**：可选
- **默认值**:
  - `RenkoAtrLength` = 10
  - `StopLossPct` = 10
  - `TakeProfitPct` = 50
- **过滤器**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: ATR
  - 止损: 可选
  - 复杂度: 基础
  - 时间框架: Renko
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中
