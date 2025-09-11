# Cycle Biologique策略
[English](README.md) | [Русский](README_ru.md)

Cycle Biologique策略基于正弦周期。当周期上穿零轴时做多，周期下穿零轴时平仓。

## 细节

- **入场条件**：周期上穿零轴。
- **出场条件**：周期下穿零轴。
- **默认参数**：
  - `CycleLength` = 30
  - `Amplitude` = 1.0
  - `Offset` = 0
- **过滤器**：
  - 类型：Cycle
  - 方向：多头
  - 指标：正弦波
  - 止损：否
  - 复杂度：低
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
