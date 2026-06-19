# Setup: Smooth Gaussian + Adaptive Supertrend (Manual Vol)
[English](README.md) | [Русский](README_ru.md)

当收盘价高于双重平滑移动平均线（“高斯”趋势）时开多。
当价格收于趋势线下方时平仓。简单的手动波动率过滤器只有在值为2或3时才允许入场。

## 详情

- **入场条件**：收盘价高于趋势线，并且（关闭波动率过滤器或波动率为2或3）。
- **多/空**：仅做多。
- **出场条件**：收盘价跌破趋势线。
- **止损**：无。
- **默认值**：
  - `TrendLength` = 75
  - `Volatility` = 2
  - `EnableVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选**：
  - 类别：趋势
  - 方向：多头
  - 指标：SMA
  - 止损：否
  - 复杂度：初级
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
