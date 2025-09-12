# 三重EMA + QQE趋势跟随策略
[English](README.md) | [Русский](README_ru.md)

该策略将两条TEMA均线与QQE过滤器结合，用于跟随趋势。
当价格位于两条TEMA之上且QQE给出看涨信号时开多单，
相反条件下开空单。
通过点数形式的移动止损保护已开仓位。

## 详情

- **入场条件**: TEMA排列与QQE上穿信号。
- **多空方向**: 双向。
- **退出条件**: 反向信号或移动止损。
- **止损**: 是。
- **默认值**:
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.238m
  - `Tema1Length` = 20
  - `Tema2Length` = 40
  - `StopLossPips` = 120
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: EMA, QQE
  - 止损: 移动
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
