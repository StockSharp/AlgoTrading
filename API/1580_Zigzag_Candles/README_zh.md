# Zigzag Candles
[English](README.md) | [Русский](README_ru.md)

该策略根据 ZigZag 枢轴点进行交易。当出现新的低点枢轴时开多仓，当出现新的高点枢轴时开空仓。

## 细节
- **入场条件**：ZigZag 枢轴点。
- **多空方向**：双向。
- **出场条件**：相反的枢轴。
- **止损**：无。
- **默认值**:
  - `ZigzagLength` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: Highest, Lowest
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险水平: 中等
