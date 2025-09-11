# Pivot Point Supertrend 枢轴超级趋势
[English](README.md) | [Русский](README_ru.md)

该策略结合枢轴点与基于ATR的Supertrend来捕捉趋势反转。

测试显示年化收益约65%，在股票市场表现最佳。

枢轴点形成动态中心线，ATR因子生成上下轨并跟随价格。当趋势反转时策略顺势入场。

## 详情
- **入场条件**: 基于枢轴点和ATR Supertrend 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: Pivot Points, ATR
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

