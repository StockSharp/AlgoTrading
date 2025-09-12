# Zig Zag Aroon Strategy
[English](README.md) | [Русский](README_ru.md)

该策略将简单的 ZigZag 枢轴识别与 Aroon 指标结合。当 Aroon Up 上穿 Aroon Down 且最近的枢轴为高点时买入。Aroon Down 上穿 Aroon Up 且最新枢轴为低点时做空。

## 细节

- **入场条件**：Aroon 交叉并匹配 ZigZag 枢轴。
- **多空方向**：双向。
- **出场条件**：反向信号。
- **止损**：无。
- **默认值**:
  - `ZigZagDepth` = 5
  - `AroonLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: Aroon, ZigZag
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险水平: 中等
