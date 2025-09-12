# RSI 与 反向加权均线策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 RSI 和反向加权移动平均的变化率过滤。当 RSI 高于阈值且均线 ROC 低于水平时做多；当 RSI 低于阈值且均线 ROC 高于水平时做空。包含基于 ATR 的追踪止损和固定比例的资金管理。

## 详情

- **入场条件**：
  - **多头**：`RSI >= RsiLongSignal` 且 `MA ROC <= RocMaLongSignal`
  - **空头**：`RSI <= RsiShortSignal` 且 `MA ROC >= RocMaShortSignal`
- **多空方向**：双向。
- **退出条件**：反向信号、止损或追踪止损。
- **止损**：是，ATR 追踪及最大亏损限制。
- **默认值**：
  - `RsiLength` = 20
  - `MaType` = RWMA
  - `MaLength` = 19
  - `RsiLongSignal` = 60
  - `RsiShortSignal` = 40
  - `TakeProfitActivation` = 5
  - `TrailingPercent` = 3
  - `MaxLossPercent` = 10
  - `FixedRatio` = 400
  - `IncreasingOrderAmount` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：RSI、移动平均、ATR
  - 止损：是
  - 复杂度：高
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
