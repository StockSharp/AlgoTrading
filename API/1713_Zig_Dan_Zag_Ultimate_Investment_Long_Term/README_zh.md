# Zig Dan Zag Ultimate Investment Long Term
[English](README.md) | [Русский](README_ru.md)

长期投资策略，结合 ZigZag 枢轴和缓慢的 SMA 趋势过滤器。当新的 ZigZag 低点形成且价格高于 SMA 时开多仓，出现相反枢轴且价格低于 SMA 时平仓。

## 细节
- **入场条件**：ZigZag 新低且高于 SMA。
- **多空方向**：仅多头。
- **出场条件**：ZigZag 新高且低于 SMA。
- **止损**：无。
- **默认值**:
  - `ZigzagDepth` = 12
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**:
  - 类别: 趋势
  - 方向: 多头
  - 指标: Highest, Lowest, SimpleMovingAverage
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 长期
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险水平: 中等
