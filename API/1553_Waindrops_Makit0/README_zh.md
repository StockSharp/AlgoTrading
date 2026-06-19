# Waindrops Makit0
[English](README.md) | [Русский](README_ru.md)

简化策略，对比自定义周期两半的VWAP。

## 细节

- **入场条件**：比较左右两半的VWAP。
- **多空方向**：双向。
- **出场条件**：相反信号。
- **止损**：否。
- **默认值**：
  - `PeriodMinutes` = 60
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 分类：成交量
  - 方向：双向
  - 指标：VWAP
  - 止损：否
  - 复杂度：基础
  - 时间框架：日内(1m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：低
