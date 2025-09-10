# 相关性数组策略
[English](README.md) | [Русский](README_ru.md)

该策略计算最多六个品种的滚动相关矩阵。它根据可配置阈值记录相关性水平，帮助评估资产之间的关系。该策略仅用于分析，不执行交易。

## 详情
- **入场条件**：无（仅分析）
- **多空方向**：无
- **出场条件**：无
- **止损**：无
- **默认值**：
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `LookbackPeriod` = 100
  - `PositiveWeak` = 0.3
  - `PositiveMedium` = 0.5
  - `PositiveStrong` = 0.7
  - `NegativeWeak` = -0.3
  - `NegativeMedium` = -0.5
  - `NegativeStrong` = -0.7
- **过滤器**：
  - 类别：统计分析
  - 方向：无
  - 指标：相关性
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：低
