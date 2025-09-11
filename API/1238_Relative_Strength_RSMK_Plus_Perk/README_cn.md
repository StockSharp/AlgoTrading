# Relative Strength RSMK Plus Perk 相对强度 RSMK
[English](README.md) | [Русский](README_ru.md)

该策略基于 Markos Katsanos 提出的相对强度 (RSMK) 指标。

当 RSMK 上穿其信号线时做多，下穿时做空。相反的交叉用于平仓。

## 详情
- **入场条件**: RSMK 与信号线交叉
- **多空方向**: 双向
- **退出条件**: 反向交叉
- **止损**: 无
- **默认值**:
  - `Period` = 90
  - `Smooth` = 3
  - `SignalPeriod` = 20
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: RSMK, EMA
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日线
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
