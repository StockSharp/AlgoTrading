# 双极理想均线策略
[English](README.md) | [Русский](README_ru.md)

该策略通过比较快速 EMA 与慢速三重 EMA 来近似 “2pb Ideal MA” 专家顾问。

## 详情

- **入场条件**：快 EMA 上穿/下穿慢 TEMA。
- **多空方向**：双向。
- **退出条件**：相反交叉时反手。
- **止损**：否。
- **默认值**：
  - `FastPeriod` = 10
  - `SlowPeriod` = 30
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**：
  - 类型: 趋势
  - 方向: 双向
  - 指标: EMA, TEMA
  - 止损: 否
  - 复杂度: 初级
  - 时间框架: 摆动 (4h)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
