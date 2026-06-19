# Gold Scalping BOS & CHoCH Strategy
[English](README.md) | [Русский](README_ru.md)

该策略在黄金上交易结构突破（BOS）和结构转变（CHoCH）形态。它计算短期支撑和阻力位，并在出现BOS后立即出现CHoCH时入场，同时使用动态止损和止盈。

## 细节

- **入场条件**：
  - **多头**：`High > LastSwingHigh` 且 `Close` 上穿 `LastSwingLow`
  - **空头**：`Low < LastSwingLow` 且 `Close` 下破 `LastSwingHigh`
- **多空方向**：双向
- **出场条件**：止损或止盈
- **止损**：动态
- **默认值**：
  - `RecentLength` = 10
  - `SwingLength` = 5
  - `TakeProfitFactor` = 2
- **过滤器**：
  - 类别：剥头皮
  - 方向：双向
  - 指标：Highest, Lowest
  - 止损：有
  - 复杂度：中等
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
