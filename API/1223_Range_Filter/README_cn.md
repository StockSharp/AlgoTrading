# Range Filter 策略
[English](README.md) | [Русский](README_ru.md)

基于范围滤波的趋势策略，当价格穿越滤波器并且方向一致时入场。

该策略计算价格变化的平滑范围，并围绕动态滤波器构建上下通道。当滤波器向上且价格突破其上方时做多；当滤波器向下且价格跌破其下方时做空。每笔交易使用固定的止损与止盈。

## 详情

- **入场条件**：价格按滤波器方向穿越。
- **多空方向**：双向。
- **退出条件**：固定止损或止盈。
- **止损方式**：固定点数。
- **默认值**：
  - `Period` = 100
  - `Multiplier` = 3
  - `RiskPoints` = 50
  - `RewardPoints` = 100
  - `UseRealisticEntry` = true
  - `SpreadBuffer` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类型: 趋势
  - 方向: 双向
  - 指标: 自定义 Range Filter
  - 止损: 固定点数
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
