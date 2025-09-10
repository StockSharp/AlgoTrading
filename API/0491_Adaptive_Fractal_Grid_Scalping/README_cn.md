# 自适应分形网格剥头皮
[English](README.md) | [Русский](README_ru.md)

该策略在最近的分形枢轴周围根据ATR距离放置限价单。趋势由简单移动平均线确定。当波动率高于阈值时，上升趋势在分形低点下方设置买入限价，下降趋势在分形高点上方设置卖出限价。退出在相反网格水平或基于ATR的止损。

## 详细信息

- **入场条件**：ATR高于阈值且价格相对SMA；在分形低点减去ATR倍数处挂买单或在分形高点加上ATR倍数处挂卖单。
- **多空方向**：双向。
- **出场条件**：相反网格水平或基于分形的止损。
- **止损**：有。
- **默认值**：
  - `AtrLength` = 14
  - `SmaLength` = 50
  - `GridMultiplierHigh` = 2.0m
  - `GridMultiplierLow` = 0.5m
  - `TrailStopMultiplier` = 0.5m
  - `VolatilityThreshold` = 1.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选器**：
  - 类别：Scalping
  - 方向：双向
  - 指标：Fractal, ATR, SMA
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
