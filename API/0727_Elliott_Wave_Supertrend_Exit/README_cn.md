# Elliott Wave Supertrend Exit 策略
[English](README.md) | [Русский](README_ru.md)

该策略在出现类似ZigZag的反转时开仓，并在Supertrend方向改变或达到固定百分比止损时平仓。

## 详情

- **入场条件**:
  - 多头：价格形成局部低点
  - 空头：价格形成局部高点
- **多空**：双向
- **出场条件**：
  - Supertrend 方向反转或触及止损
- **止损**：入场价的固定百分比
- **默认值**：
  - `WaveLength` = 4
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3
  - `StopLossPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：Highest、Lowest、SuperTrend
  - 止损：是
  - 复杂度：中等
  - 时间框架：短期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
