# Robot Danu
[English](README.md) | [Русский](README_ru.md)

该策略比较由蜡烛高低点计算的快慢 ZigZag 水平。
当快 ZigZag 水平高于慢水平时做空，
当快 ZigZag 水平低于慢水平时做多。

## 细节
- **入场条件**：快慢 ZigZag 枢轴的比较。
- **多空方向**：双向。
- **出场条件**：相反的 ZigZag 关系。
- **止损**：无。
- **默认值**：
  - `FastLength` = 28
  - `SlowLength` = 56
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 分类：趋势
  - 方向：双向
  - 指标：Highest，Lowest
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
