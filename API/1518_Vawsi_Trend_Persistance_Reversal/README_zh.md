# VAWSI 和趋势持久性反转策略
[English](README.md) | [Русский](README_ru.md)

该反转策略结合 VAWSI、趋势持久性和 ATR，在平均K线图上构建动态阈值。

## 细节

- **入场条件**：平均K线收盘价上穿/下穿动态阈值
- **多空方向**：双向
- **出场条件**：反向穿越或保护性止损
- **止损**：是，按百分比
- **默认值**：
  - `CandleType` = 15 分钟
  - `SlTp` = 5
  - `RsiWeight` = 100
  - `TrendWeight` = 79
  - `AtrWeight` = 20
  - `CombinationMult` = 1
  - `Smoothing` = 3
  - `CycleLength` = 20
- **过滤器**：
  - 类别：反转
  - 方向：双向
  - 指标：RSI、ATR
  - 止损：是
  - 复杂度：高级
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险水平：中等
