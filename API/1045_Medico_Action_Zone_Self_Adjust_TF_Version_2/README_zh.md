# Medico Action Zone Self Adjust TF Version 2
[English](README.md) | [Русский](README_ru.md)

基于EMA交叉并由高时间框架确认的策略。当快速EMA上穿慢速EMA且高时间框架收盘价位于快速EMA之上时开仓；反向条件触发平仓或反向。

## 细节

- **入场条件**：快速EMA上穿慢速EMA且高时间框架收盘价高于快速EMA。
- **多/空**：双向。
- **出场条件**：相反交叉并得到确认。
- **止损**：无。
- **默认值**：
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
- **筛选**：
  - 分类：趋势
  - 方向：双向
  - 指标：EMA
  - 止损：否
  - 复杂度：基础
  - 时间框架：日线
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
