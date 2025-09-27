# 变动率策略
[English](README.md) | [Русский](README_ru.md)

该策略利用变动率指标识别泡沫并在穿越零线时入场，同时根据收益动态调整仓位。

回测显示在主要资产的日线数据上表现稳定。

## 详情

- **入场条件**：ROC 上穿或下破零线；泡沫破裂时可做空。
- **多空方向**：双向。
- **退出条件**：反向信号或止损。
- **止损**：是。
- **默认值**：
  - `RocLength` = 365
  - `BubbleThreshold` = 180m
  - `StopLossPercent` = 6m
  - `FixedRatioValue` = 400m
  - `IncreasingOrderAmount` = 200m
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**：
  - 类型：动量
  - 方向：双向
  - 指标：RateOfChange
  - 止损：是
  - 复杂度：中等
  - 时间框架：日线
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
