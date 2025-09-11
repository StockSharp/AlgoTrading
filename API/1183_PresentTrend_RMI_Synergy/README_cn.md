# PresentTrend RMI 协同
[English](README.md) | [Русский](README_ru.md)

该策略将基于 RSI 的动量过滤与 SuperTrend 风格的 ATR 跟踪止损结合。当动量超过阈值且价格符合趋势方向时入场。止损利用移动平均和 ATR 带动态追踪价格。

回测表明该策略在加密等趋势性市场表现稳定。

## 详情

- **入场条件**: 多头 - RMI > 60 且价格高于均线；空头 - RMI < 40 且价格低于均线。
- **多空方向**: 两个方向。
- **出场条件**: 基于 ATR 的跟踪止损。
- **止损**: 有。
- **默认值**:
  - `RmiPeriod` = 21
  - `SuperTrendLength` = 5
  - `SuperTrendMultiplier` = 4.0m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: Both
  - 指标: RSI, ATR, SMA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

