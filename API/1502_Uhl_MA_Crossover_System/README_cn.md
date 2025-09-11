# Uhl MA Crossover System
[English](README.md) | [Русский](README_ru.md)

Uhl MA Crossover System 使用方差调节平滑度构建两条自适应线 CTS 与 CMA。当 CTS 上穿 CMA 时开多，下穿时开空。

## 细节

- **入场条件**：CTS 上穿 CMA。
- **多空方向**：双向。
- **出场条件**：CTS 下穿 CMA。
- **止损**：无。
- **默认值**：
  - `Length` = 100
  - `Multiplier` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：Trend
  - 方向：Both
  - 指标：SMA, Variance
  - 止损：无
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
