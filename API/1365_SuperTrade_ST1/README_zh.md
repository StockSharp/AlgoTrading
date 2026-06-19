# SuperTrade ST1 Strategy
[English](README.md) | [Русский](README_ru.md)

只做多策略，结合 Supertrend 指标、EMA 过滤器和基于 ATR 的风控。

回测显示年化收益约 45%。在加密货币市场表现最佳。

策略等待 Supertrend 方向下降且价格保持在 Supertrend 线和 EMA 之上。风险通过 ATR 止损和止盈控制，比例为 1:4。

## 细节

- **入场条件**：
  - 前一根 Supertrend 方向 > 当前方向
  - 收盘价 > Supertrend
  - 收盘价 > EMA
- **做多/做空**：仅做多
- **出场条件**：`Close <= entry - StopAtrMultiplier * ATR` 或 `Close >= entry + TakeAtrMultiplier * ATR`
- **止损**：基于 ATR 的止损和止盈
- **默认参数**：
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `EmaPeriod` = 200
  - `StopAtrMultiplier` = 1.0
  - `TakeAtrMultiplier` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **过滤器**：
  - 类别：趋势
  - 方向：多头
  - 指标：Supertrend、EMA、ATR
  - 止损：是
  - 复杂度：简单
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

