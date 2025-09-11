# 三重MA高阶时间框架动态平滑策略
[English](README.md) | [Русский](README_ru.md)

该策略比较在更高时间框架上计算的三条移动平均线。
每条均线根据其时间框架与基础时间框架的比例进行平滑处理。
当第一条均线与第二条均线交叉且第三条均线确认方向时产生信号。

## 详情

- **入场条件**: MA1与MA2交叉并由MA3确认趋势。
- **多空方向**: 双向。
- **退出条件**: 反向信号。
- **止损**: 无。
- **默认值**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HigherTimeFrame1` = TimeSpan.FromMinutes(15)
  - `HigherTimeFrame2` = TimeSpan.FromMinutes(60)
  - `HigherTimeFrame3` = TimeSpan.FromMinutes(240)
  - `Length1` = 21
  - `Length2` = 21
  - `Length3` = 50
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: MA
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内 (基础5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
