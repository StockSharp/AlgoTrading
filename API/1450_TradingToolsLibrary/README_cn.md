# Trading Tools Library Strategy
[English](README.md) | [Русский](README_ru.md)

简单的SMA交叉策略，结合RSI过滤和入场冷却时间。

## 细节
- **入场条件**：
  - **多头**：快SMA上穿慢SMA且RSI低于`RsiUpper`
  - **空头**：快SMA下穿慢SMA且RSI高于`RsiLower`
- **多空方向**：双向
- **出场条件**：
  - 反向信号
- **止损**：无
- **默认参数**：
  - `ShortLength` = 10
  - `LongLength` = 30
  - `RsiLength` = 14
  - `CooldownBars` = 3
  - `RsiUpper` = 60
  - `RsiLower` = 40
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：SMA、RSI
  - 止损：无
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
