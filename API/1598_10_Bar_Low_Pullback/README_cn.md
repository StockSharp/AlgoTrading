# Short Only 10 Bar Low Pullback 策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格跌破前 N 根K线的最低点且 IBS 高于阈值时做空，可选的 EMA 过滤器用于确认下行趋势。

## 细节

- **入场条件**：
  - 当前低点跌破 `LowestPeriod` 根K线的最低值。
  - IBS > `IbsThreshold`。
  - 可选：启用过滤器时，收盘价低于 EMA。
  - 时间位于 `StartTime` 和 `EndTime` 之间。
- **方向**：仅做空。
- **出场条件**：
  - 收盘价低于前一根K线低点时平仓。
- **止损**：无。
- **默认参数**：
  - `LowestPeriod` = 10
  - `IbsThreshold` = 0.85
  - `UseEmaFilter` = true
  - `EmaPeriod` = 200
- **过滤器**：
  - 类型：回调
  - 方向：空头
  - 指标：Lowest, EMA
  - 止损：否
  - 复杂度：低
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
